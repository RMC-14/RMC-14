using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Damage;

/// <summary>
/// Applies status effects when specified damage groups are equal or greater to a threshold.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCApplyOnDamageThresholdComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan CheckEvery = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan NextCheck;

    [DataField, AutoNetworkedField]
    public List<RMCDamageThresholdEffect> Thresholds = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public partial struct RMCDamageThresholdEffect()
{
    [DataField]
    public ProtoId<DamageGroupPrototype> DamageType;

    [DataField]
    public FixedPoint2 Threshold;

    [DataField]
    public string StatusName = String.Empty;

    [DataField]
    public string StatusComponent = String.Empty;

    [DataField]
    public TimeSpan Duration;

}
