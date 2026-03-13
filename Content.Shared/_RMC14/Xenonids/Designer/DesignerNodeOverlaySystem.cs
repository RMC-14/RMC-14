using Robust.Shared.GameObjects;
using Robust.Shared.Network;

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
        if (overlayComp.Overlay is { } existing && !Deleted(existing))
            return;

        var isDoor = nodeComp.ConstructIsDoor;
        var overlayProto = nodeComp.NodeType switch
        {
            DesignNodeType.Optimized => isDoor ? overlayComp.OptimizedDoorProto : overlayComp.OptimizedWallProto,
            DesignNodeType.Flexible => isDoor ? overlayComp.FlexibleDoorProto : overlayComp.FlexibleWallProto,
            DesignNodeType.Construct => isDoor ? overlayComp.ConstructDoorProto : overlayComp.ConstructWallProto,
            _ => overlayComp.ConstructWallProto,
        };

        var coords = Transform(nodeUid).Coordinates;
        var overlay = Spawn(overlayProto, coords);
        _transform.SetParent(overlay, nodeUid);

        overlayComp.Overlay = overlay;
    }

    public void CleanupOverlay(EntityUid nodeUid, DesignNodeComponent? nodeComp = null)
    {
        if (!_net.IsServer)
            return;

        if (!TryComp(nodeUid, out DesignNodeOverlayComponent? overlayComp))
            return;

        if (overlayComp.Overlay is { } existing && !Deleted(existing))
            QueueDel(existing);

        overlayComp.Overlay = null;
    }

    private void OnOverlayTerminating(Entity<DesignNodeOverlayComponent> node, ref EntityTerminatingEvent args)
    {
        CleanupOverlay(node.Owner);
    }
}
