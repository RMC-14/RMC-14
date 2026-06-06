using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class XenoDespoilerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool NextAbilityEmpowered;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan EmpowerExpiresAt;

    [DataField]
    public List<DamageSpecifier> FinishingStabBonusByTier = new();

    [DataField]
    public ComponentRegistry AcidComponents = new();
}
