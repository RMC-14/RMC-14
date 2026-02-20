using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.NightVision;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client._RMC14.NightVision;

public sealed class NightVisionPreferencesSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(RMCCVars.RMCNightVisionColor, _ => OnNightVisionColorChanged(), true);
    }

    public NightVisionColor GetPreferredColor()
    {
        var stored = _cfg.GetCVar(RMCCVars.RMCNightVisionColor);
        return ParseColor(stored);
    }

    public void ApplyPreferredColor()
    {
        if (_player.LocalPlayer?.ControlledEntity is not { } player)
            return;

        var color = GetPreferredColor();
        var pref = EnsureComp<NightVisionPreferencesComponent>(player);

        if (pref.Color == color)
            return;

        pref.Color = color;
        Dirty(player, pref);
    }

    public void SetPreferredColor(NightVisionColor color)
    {
        _cfg.SetCVar(RMCCVars.RMCNightVisionColor, color.ToString());
        ApplyPreferredColor();
    }

    private void OnNightVisionColorChanged()
    {
        ApplyPreferredColor();

        if (EntityManager.TrySystem(out NightVisionSystem? nightVision))
            nightVision.RefreshNightVisionColor();
    }

    private static NightVisionColor ParseColor(string value)
    {
        return Enum.TryParse(value, out NightVisionColor color)
            ? color
            : NightVisionColor.Green;
    }
}
