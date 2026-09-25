using Content.Shared._RMC14.Marines.Mutiny;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Marines.Mutiny;

[RegisterComponent, Access(typeof(MutinyRuleSystem))]
public sealed partial class MutinyRuleComponent : Component
{
    [DataField]
    public MutinyPhase Phase = MutinyPhase.Recruiting;

    [DataField]
    public ProtoId<NpcFactionPrototype> Faction = "UNMC";

    [DataField]
    public EntProtoId<IFFFactionComponent> IffFaction = "FactionMarine";

    [DataField]
    public TimeSpan ChoiceDuration = TimeSpan.FromSeconds(20);
}
