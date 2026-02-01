using Content.Shared.FixedPoint;
using Content.Shared.DoAfter;
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
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DesignerStrainComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MaxDesignNodes = 36;

    [DataField, AutoNetworkedField]
    public int CurrentDesignNodes;

    [DataField, AutoNetworkedField]
    public bool BuildDoorNodes;

    public int NextDesignNodeOrder;
    public TimeSpan NextRemoteThickenAt;
    public TimeSpan NextGreaterResinSurgeAt;
    public DoAfterId? GreaterResinSurgeDoAfter;
    public readonly List<EntityUid> GreaterResinSurgeEffects = new();
}
