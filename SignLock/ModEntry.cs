using System;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace SignLock;

internal sealed class ModEntry : Mod
{
    private static ModEntry Instance = null!;
    private ModConfig Config = null!;
    private const int HintDuration = 120;
    private const int HintFade = 30;
    private string? HintText;
    private int HintEndTick;

    public override void Entry(IModHelper helper)
    {
        Instance = this;
        Config = helper.ReadConfig<ModConfig>();

        var harmony = new Harmony(ModManifest.UniqueID);
        harmony.Patch(
            original: AccessTools.Method(typeof(Sign), nameof(Sign.checkForAction)),
            prefix: new HarmonyMethod(typeof(ModEntry), nameof(Before_CheckForAction))
        );

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Display.RenderedHud += OnRenderedHud;
    }

    /// <summary>Draw the lock hint bottom-left, a bit above where vanilla HUD messages sit so it clears the toolbar.</summary>
    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        int left = HintEndTick - Game1.ticks;
        if (HintText is null || left <= 0)
            return;

        float alpha = Math.Min(1f, left / (float)HintFade);
        const int pad = 20;
        Vector2 size = Game1.smallFont.MeasureString(HintText);
        int width = (int)size.X + pad * 2;
        int height = (int)size.Y + pad * 2;
        int x = 16;
        int y = Game1.uiViewport.Height - 136 - height;

        IClickableMenu.drawTextureBox(e.SpriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White * alpha, drawShadow: false);
        Utility.drawTextWithShadow(e.SpriteBatch, HintText, Game1.smallFont, new Vector2(x + pad, y + pad), Game1.textColor * alpha);
    }

    /// <summary>Turn e.g. "LeftShift, RightShift" into "Shift".</summary>
    private static string DescribeKey(KeybindList keys)
    {
        var names = keys.Keybinds
            .Where(k => k.IsBound)
            .Select(k => string.Join(" + ", k.Buttons.Select(b => Regex.Replace(b.ToString(), "^(Left|Right)(?=[A-Z])", ""))))
            .Distinct()
            .ToArray();
        return names.Length > 0 ? string.Join(" / ", names) : keys.ToString();
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (gmcm is null)
            return;

        gmcm.Register(ModManifest, reset: () => Config = new ModConfig(), save: () => Helper.WriteConfig(Config));
        gmcm.AddBoolOption(ModManifest, () => Config.Enabled, v => Config.Enabled = v,
            () => Helper.Translation.Get("config.enabled.name"), () => Helper.Translation.Get("config.enabled.tooltip"));
        gmcm.AddKeybindList(ModManifest, () => Config.UnlockKey, v => Config.UnlockKey = v,
            () => Helper.Translation.Get("config.unlock-key.name"), () => Helper.Translation.Get("config.unlock-key.tooltip"));
        gmcm.AddBoolOption(ModManifest, () => Config.ShowHint, v => Config.ShowHint = v,
            () => Helper.Translation.Get("config.show-hint.name"), () => Helper.Translation.Get("config.show-hint.tooltip"));
    }

    /// <summary>Swallow the click if the sign already shows an item and the unlock key isn't held.</summary>
    private static bool Before_CheckForAction(Sign __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
    {
        try
        {
            var config = Instance.Config;
            if (!config.Enabled || justCheckingForActivity || !who.IsLocalPlayer)
                return true;
            if (__instance.displayItem.Value is null || who.ActiveItem is null)
                return true;
            if (config.UnlockKey.IsDown())
                return true;

            if (config.ShowHint)
            {
                Instance.HintText = Instance.Helper.Translation.Get("hint.locked", new { key = DescribeKey(config.UnlockKey) });
                Instance.HintEndTick = Game1.ticks + HintDuration;
            }

            __result = true;
            return false;
        }
        catch (Exception ex)
        {
            Instance.Monitor.Log($"Failed in {nameof(Before_CheckForAction)}:\n{ex}", LogLevel.Error);
            return true;
        }
    }
}
