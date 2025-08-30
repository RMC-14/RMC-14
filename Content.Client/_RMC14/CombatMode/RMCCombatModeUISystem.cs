using Content.Client.CombatMode;
using Content.Client.Hands.Systems;
using Content.Shared._RMC14.CombatMode;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._RMC14.CombatMode;

public sealed class RMCCombatModeUISystem : EntitySystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly CombatModeSystem _combatMode = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly RMCCombatModeSystem _rmcCombatMode = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private bool _crosshairsEnabled;
    private ICursor? _crosshairCursor;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_config, CCVars.CombatModeIndicatorsPointShow, v => _crosshairsEnabled = v, true);
    }

    public override void FrameUpdate(float frameTime)
    {
        if (_ui.CurrentlyHovered is not IViewportControl)
            return;

        if (_crosshairsEnabled &&
            _combatMode.IsInCombatMode() &&
            _hands.GetActiveHandEntity() is { } held &&
            _rmcCombatMode.GetCrosshair(held) != null)
        {
            _crosshairCursor ??= _clyde.CreateCursor(new Image<Rgba32>(1, 1), Vector2i.One);
            _ui.CurrentlyHovered.CustomCursorShape = _crosshairCursor;
        }
        else
        {
            _ui.CurrentlyHovered.CustomCursorShape = null;
        }
    }
}
