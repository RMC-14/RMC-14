using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Damage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(SharedRMCDamageableSystem))]
public sealed partial class DamageMobStateComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier CritDamage = new();

    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> StopCritDamageReagent = "CMInaprovaline";

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan DamageAt;
}
