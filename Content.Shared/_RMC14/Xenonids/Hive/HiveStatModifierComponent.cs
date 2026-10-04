using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hive;

[RegisterComponent, NetworkedComponent]
[Access(typeof(HiveStatModifierSystem), typeof(XenoSystem))]
public sealed partial class HiveStatModifierComponent : Component
{
    [DataField]
    public DamageSpecifier DamageIncrease = new();

    [DataField]
    public float HealthMultiplier = 1f;

    [DataField]
    public float PlasmaGainMultiplier = 1f;

    [DataField]
    public float SpeedMultiplier = 1f;
}
