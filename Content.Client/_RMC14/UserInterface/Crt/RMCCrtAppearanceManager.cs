using Content.Shared._RMC14.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client._RMC14.UserInterface.Crt;

internal interface IRMCCrtAppearanceManager
{
    RMCCrtAppearanceSettings Settings { get; }

    event Action<RMCCrtAppearanceSettings>? AppearanceChanged;
}

internal sealed class RMCCrtAppearanceManager : IRMCCrtAppearanceManager, IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    public RMCCrtAppearanceSettings Settings { get; private set; } = new(true, true);

    public event Action<RMCCrtAppearanceSettings>? AppearanceChanged;

    public void PostInject()
    {
        _configuration.OnValueChanged(RMCCVars.RMCCrtThemeEnabled, OnThemeEnabledChanged, true);
        _configuration.OnValueChanged(RMCCVars.RMCCrtEffectsEnabled, OnEffectsEnabledChanged, true);
    }

    private void OnThemeEnabledChanged(bool enabled)
    {
        UpdateSettings(new RMCCrtAppearanceSettings(enabled, Settings.EffectsEnabled));
    }

    private void OnEffectsEnabledChanged(bool enabled)
    {
        UpdateSettings(new RMCCrtAppearanceSettings(Settings.ThemeEnabled, enabled));
    }

    private void UpdateSettings(RMCCrtAppearanceSettings settings)
    {
        if (Settings == settings)
            return;

        Settings = settings;
        AppearanceChanged?.Invoke(settings);
    }
}
