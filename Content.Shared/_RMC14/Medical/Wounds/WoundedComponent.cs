using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWoundsSystem))]
public sealed partial class WoundedComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> BruteWoundGroup = "Brute";

    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> BurnWoundGroup = "Burn";

    [DataField, AutoNetworkedField]
    public List<Wound> Wounds = new();

    [DataField, AutoNetworkedField]
    public FixedPoint2 PassiveHealing = FixedPoint2.New(-0.05f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan UpdateAt;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(1);
}

[DataRecord]
[Serializable, NetSerializable]
public record struct Wound(
    FixedPoint2 Damage,
    FixedPoint2 Healed,
    float Bloodloss,
    [field: DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    TimeSpan? StopBleedAt,
    WoundType Type,
    bool Treated
);

[Serializable, NetSerializable]
public enum WoundType
{
    Brute = 0,
    Burn,
    Surgery
}
