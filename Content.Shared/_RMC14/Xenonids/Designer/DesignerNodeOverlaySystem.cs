using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Designer;
// Overlays for design nodes to indicate their type. Visible to xenos only
public sealed class DesignerNodeOverlaySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public void EnsureOverlay(EntityUid nodeUid, DesignNodeComponent nodeComp)
    {
        if (!_net.IsServer)
            return;

        if (nodeComp.OverlayEntity.IsValid() && !Deleted(nodeComp.OverlayEntity))
            return;

        if (nodeComp.OverlayPrototype is not { } overlayProto)
            return;

        var coords = Transform(nodeUid).Coordinates;
        var overlay = Spawn(overlayProto, coords);
        _transform.SetParent(overlay, nodeUid);

        nodeComp.OverlayEntity = overlay;
    }

    public void CleanupOverlay(EntityUid nodeUid, DesignNodeComponent nodeComp)
    {
        if (!_net.IsServer)
            return;

        if (nodeComp.OverlayEntity.IsValid() && !Deleted(nodeComp.OverlayEntity))
            QueueDel(nodeComp.OverlayEntity);

        nodeComp.OverlayEntity = EntityUid.Invalid;
    }
}
