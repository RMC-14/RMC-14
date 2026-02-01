using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Designer;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DesignNodeComponent : Component
{

    [DataField, AutoNetworkedField]
    public DesignNodeType NodeType = DesignNodeType.Construct;
    // Optimized nodes reduce build time by half
    [DataField, AutoNetworkedField]
    public float OptimizedBuildTimeMultiplier = 0.5f;

    // Flexible nodes reduce plasma cost by half
    [DataField, AutoNetworkedField]
    public float FlexiblePlasmaCostMultiplier = 0.5f;

    [DataField, AutoNetworkedField]
    public EntityUid? BoundXeno;

    // The weed entity this node is bound to
    [DataField, AutoNetworkedField]
    public EntityUid? BoundWeed;

    // The hive number this node belongs to.
    [DataField, AutoNetworkedField]
    public int HiveNumber = -1;

    [DataField, AutoNetworkedField]
    public string DesignMark = "resin-wall";

    // Used for enforcing "oldest node is deleted" when exceeding the cap.
    public int PlacedOrder;
}
