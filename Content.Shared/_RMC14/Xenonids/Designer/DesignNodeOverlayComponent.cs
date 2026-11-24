using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Designer;

[Access(typeof(DesignerNodeOverlaySystem), typeof(DesignerConstructNodeSystem))]
[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class DesignNodeOverlayComponent : Component
{
    // Runtime-only reference to the spawned mark entity.
    // Intentionally not a DataField/AutoNetworkedField so prototype save tests don't serialize EntityUids.
    public EntityUid? Overlay;

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId OptimizedWallProto = "DesignNodeMarkOptimizedWall";

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId OptimizedDoorProto = "DesignNodeMarkOptimizedDoor";

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId FlexibleWallProto = "DesignNodeMarkFlexibleWall";

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId FlexibleDoorProto = "DesignNodeMarkFlexibleDoor";

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId ConstructWallProto = "DesignNodeMarkConstructWall";

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId ConstructDoorProto = "DesignNodeMarkConstructDoor";
}
