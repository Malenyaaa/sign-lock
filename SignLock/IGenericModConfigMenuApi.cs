using System;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace SignLock;

/// <summary>The subset of the Generic Mod Config Menu API used by this mod.</summary>
public interface IGenericModConfigMenuApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

    void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);

    void AddKeybindList(IManifest mod, Func<KeybindList> getValue, Action<KeybindList> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
}
