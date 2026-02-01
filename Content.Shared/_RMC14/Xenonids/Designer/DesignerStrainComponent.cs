using Content.Shared.FixedPoint;
using Content.Shared._RMC14.Xenonids.Construction;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Designer;

// Designers trade direct construction ability for remote node influence.
// They can place up to 36 design nodes that modify construction behavior:
// - Optimized nodes: 50% faster building
// - Flexible nodes: 50% cheaper plasma cost
// - Construct nodes: Allow any hive member to donate plasma for collaborative building
// - Design nodes can be walls or doors (cosmetic only; both allow same structures).
// - Designers can remotely thicken walls/doors within range of their nodes every 60 seconds.
// - Greater Resin Surge: Converts all nearby nodes into unstable resin. TODO: Make this reflective resin
[Access(typeof(SharedXenoConstructionSystem), typeof(DesignerGreaterResinSurgeSystem), typeof(DesignerNodeBindingSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DesignerStrainComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MaxDesignNodes = 36;

    [DataField, AutoNetworkedField]
    public int CurrentDesignNodes;

    [DataField, AutoNetworkedField]
    public bool BuildDoorNodes;

    [DataField, AutoNetworkedField]
    public int NextDesignNodeOrder;

    [DataField, AutoNetworkedField]
    public TimeSpan NextRemoteThickenAt;

    [DataField, AutoNetworkedField]
    public TimeSpan NextGreaterResinSurgeAt;

    [DataField, AutoNetworkedField]
    public FixedPoint2 GreaterResinSurgePlasmaCost = 250;

    [DataField, AutoNetworkedField]
    public TimeSpan GreaterResinSurgeCooldown = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public float GreaterResinSurgeRange = 7f;

    [DataField, AutoNetworkedField]
    public TimeSpan GreaterResinSurgeBuildTime = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public EntProtoId GreaterResinSurgeAnimationEffect = "RMCEffectWallXenoResinThick";

    [DataField, AutoNetworkedField]
    public EntProtoId GreaterResinSurgeWallPrototype = "WallXenoResinThickSurge";
}
