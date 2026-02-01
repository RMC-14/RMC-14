using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Designer;
// Overlays for design nodes to indicate their type. Visible to xenos only
public sealed class DesignerNodeOverlaySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DesignNodeOverlayComponent, EntityTerminatingEvent>(OnOverlayTerminating);
    }

    public void EnsureOverlay(EntityUid nodeUid, DesignNodeComponent nodeComp)
    {
        if (!_net.IsServer)
            return;

        var overlayComp = EnsureComp<DesignNodeOverlayComponent>(nodeUid);
        if (overlayComp.Overlay.IsValid() && !Deleted(overlayComp.Overlay))
            return;

        if (nodeComp.OverlayPrototype is not { } overlayProto)
            return;

        if (!TryComp(nodeUid, out TransformComponent? nodeXform))
            return;

        var coords = nodeXform.Coordinates;
        var overlay = Spawn(overlayProto, coords);
        _transform.SetParent(overlay, nodeUid);

        overlayComp.Overlay = overlay;
    }

    private void OnOverlayTerminating(Entity<DesignNodeOverlayComponent> node, ref EntityTerminatingEvent args)
    {
        if (!_net.IsServer)
            return;

        if (node.Comp.Overlay.IsValid() && !Deleted(node.Comp.Overlay))
            QueueDel(node.Comp.Overlay);
    }
}
