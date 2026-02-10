using Content.Client.UserInterface.Systems.Actions;
using Content.Shared._RMC14.CombatMode;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Designer;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._RMC14.Xenonids.Designer;

public sealed class DesignerCrosshairSystem : EntitySystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private ICursor? _transparentCursor;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new DesignerCrosshairOverlay(_input, EntityManager, _eye, _player));
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<DesignerCrosshairOverlay>();
    }

    public override void FrameUpdate(float frameTime)
    {
        if (_ui.CurrentlyHovered is not IViewportControl)
            return;

        if (_player.LocalEntity is not { } player ||
            GetDesignerCrosshair(player) == null)
        {
            return;
        }

        // Hide the OS cursor so the overlay can draw the designer crosshair instead.
        _transparentCursor ??= _clyde.CreateCursor(new Image<Rgba32>(1, 1), Vector2i.One);
        _ui.CurrentlyHovered.CustomCursorShape = _transparentCursor;
    }

    public SpriteSpecifier.Rsi? GetDesignerCrosshair(EntityUid player)
    {
        if (!TryComp(player, out DesignerCrosshairComponent? crosshair))
            return null;

        if (!TryComp(player, out DesignerStrainComponent? designer))
            return null;

        var actionController = _ui.GetUIController<ActionUIController>();
        if (actionController.SelectingTargetFor is not { } selectedActionId)
            return null;

        if (!HasComp<XenoConstructionActionComponent>(selectedActionId))
            return null;

        return designer.BuildDoorNodes ? crosshair.DoorRsi : crosshair.WallRsi;
    }
}
