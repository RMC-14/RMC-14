using Content.Shared.Damage;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Hive;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(RenegadeDefectionSystem))]
public sealed partial class RenegadeDefectionPendingComponent : Component
{
    [DataField]
    public ProtoId<NpcFactionPrototype> BrokenFaction;

    [DataField]
    public TimeSpan ChoiceTimeout = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ExpiresAt;

    [DataField]
    public DamageSpecifier RipDamage = new() { DamageDict = { ["Blunt"] = 50 } };
}
