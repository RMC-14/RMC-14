using Content.Shared.FixedPoint;
using Content.Shared._RMC14.Xenonids.Construction;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Designer;

[Access(typeof(SharedXenoConstructionSystem), typeof(DesignerNodeBindingSystem), typeof(DesignerConstructNodeSystem), typeof(DesignerNodeOverlaySystem))]
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

    [DataField, AutoNetworkedField]
    public EntProtoId? OverlayPrototype;

    // Runtime-only spawned overlay entity. Not serialized or networked.
    [Access(typeof(DesignerNodeOverlaySystem))]
    public EntityUid OverlayEntity = EntityUid.Invalid;

    // Used for enforcing "oldest node is deleted" when exceeding the cap.
    public int PlacedOrder;

    [DataField, AutoNetworkedField]
    public TimeSpan ConstructBuildTime = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public FixedPoint2 ConstructPlasmaCost = FixedPoint2.New(70);

    [DataField, AutoNetworkedField]
    public bool ConstructIsDoor;

    [DataField, AutoNetworkedField]
    public EntProtoId ConstructWeedbound = "WallXenoResinWeedbound";

    [DataField, AutoNetworkedField]
    public EntProtoId ConstructWeedboundThick = "WallXenoResinThickWeedbound";

    [DataField, AutoNetworkedField]
    public EntProtoId ConstructAnimationEffect = "RMCEffectWallXenoResin";

    [DataField, AutoNetworkedField]
    public EntProtoId ConstructAnimationEffectThick = "RMCEffectWallXenoResinThick";

    [DataField, AutoNetworkedField]
    public SoundSpecifier ConstructBuildSound = new SoundCollectionSpecifier("RMCResinBuild")
    {
        Params = AudioParams.Default.WithVolume(-8f),
    };
}
