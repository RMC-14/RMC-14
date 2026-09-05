using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Paratoxin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParatoxinAffectedComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Stacks;


    [DataField, AutoNetworkedField]
    public int MaxStacks = 30;

    [DataField, AutoNetworkedField]
    public TimeSpan EffectEvery = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan DecrementEvery = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan DecrementGraceTime = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan NextEffectTime;

    [DataField, AutoNetworkedField]
    public TimeSpan NextDecrementTime;

    [DataField, AutoNetworkedField]
    public FixedPoint2 DamagePerStack = 0.2;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxDamagePerEffect = 5;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxDamageBase = 10;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxDamageBonus = 30;

    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> DamageGroup = "Airloss";

    [DataField, AutoNetworkedField]
    public ProtoId<DamageTypePrototype> DamageType = "Asphyxiation";
}

[Serializable, NetSerializable]
public enum ParatoxinVisualLayers
{
    Base,
}

[Serializable, NetSerializable]
public enum ParatoxinVisuals
{
    Stacks,
}
