using Robust.Shared.Prototypes;
using Content.Shared.NPC.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Hive;

[RegisterComponent, Access(typeof(RenegadeDefectionSystem))]
public sealed partial class RenegadeDefectionPendingComponent : Component
{
    [DataField]
    public ProtoId<NpcFactionPrototype> BrokenFaction;
}
