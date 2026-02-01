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
        if (overlayComp.Overlay.IsValid() && !Deleted(overlayComp.Overlay))
            return;

        if (string.IsNullOrWhiteSpace(nodeComp.DesignMark))
            return;

        var overlayProto = GetOverlayProto(nodeComp.DesignMark);
        if (overlayProto == null)
            return;

        var coords = Transform(nodeUid).Coordinates;
        var overlay = Spawn(overlayProto, coords);
        _transform.SetParent(overlay, nodeUid);

        overlayComp.Overlay = overlay;
    }

    private static string? GetOverlayProto(string designMark)
    {
        if (string.Equals(designMark, "wall_mark_optimized", StringComparison.OrdinalIgnoreCase))
            return "DesignNodeMarkOptimizedWall";
        if (string.Equals(designMark, "door_mark_optimized", StringComparison.OrdinalIgnoreCase))
            return "DesignNodeMarkOptimizedDoor";

        if (string.Equals(designMark, "wall_mark_flex", StringComparison.OrdinalIgnoreCase))
            return "DesignNodeMarkFlexibleWall";
        if (string.Equals(designMark, "door_mark_flex", StringComparison.OrdinalIgnoreCase))
            return "DesignNodeMarkFlexibleDoor";

        if (string.Equals(designMark, "wall_mark_construct", StringComparison.OrdinalIgnoreCase))
            return "DesignNodeMarkConstructWall";
        if (string.Equals(designMark, "door_mark_construct", StringComparison.OrdinalIgnoreCase))
            return "DesignNodeMarkConstructDoor";

        return null;
    }

    private void OnOverlayTerminating(Entity<DesignNodeOverlayComponent> node, ref EntityTerminatingEvent args)
    {
        if (!_net.IsServer)
            return;

        if (node.Comp.Overlay.IsValid() && !Deleted(node.Comp.Overlay))
            QueueDel(node.Comp.Overlay);
    }
}
