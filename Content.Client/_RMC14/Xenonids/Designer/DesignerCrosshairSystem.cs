using Content.Client.UserInterface.Systems.Actions;
using Content.Shared._RMC14.CombatMode;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Designer;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._RMC14.Xenonids.Designer;

public sealed class DesignerCrosshairSystem : EntitySystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
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

        if (!TryComp(player, out XenoConstructionComponent? construction))
            return null;

        var actionController = _ui.GetUIController<ActionUIController>();
        if (actionController.SelectingTargetFor is not { } selectedActionId)
            return null;

        if (!HasComp<XenoConstructionActionComponent>(selectedActionId))
            return null;

        // Resolve the node type from the current build choice prototype.
        var nodeType = DesignNodeType.Construct;
        if (construction.BuildChoice is { } buildChoice &&
            _prototype.TryIndex(buildChoice, out var proto) &&
            proto.TryGetComponent(out DesignNodeComponent? nodeComp, _compFactory))
        {
            nodeType = nodeComp.NodeType;
        }

        var isDoor = designer.BuildDoorNodes;

        return nodeType switch
        {
            DesignNodeType.Optimized => isDoor ? crosshair.OptimizedDoorRsi : crosshair.OptimizedWallRsi,
            DesignNodeType.Flexible => isDoor ? crosshair.FlexibleDoorRsi : crosshair.FlexibleWallRsi,
            DesignNodeType.Construct => isDoor ? crosshair.ConstructDoorRsi : crosshair.ConstructWallRsi,
            _ => null,
        };
    }
}
