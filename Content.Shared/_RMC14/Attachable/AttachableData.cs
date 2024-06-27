using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization;


namespace Content.Shared._RMC14.Attachable;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class AttachableSlot
{
    [DataField]
    public bool Locked = false;
    
    [DataField]
    public EntityWhitelist Whitelist;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class AttachableWeaponMeleeModifierSet;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class AttachableWeaponRangedModifierSet
{
    [DataField]
    public int ShotsPerBurst;

    [DataField]
    public FixedPoint2 DamageFlat = FixedPoint2.Zero;

    [DataField]
    public float RecoilFlat;

    [DataField]
    public double AngleIncrease = 1.0;

    [DataField]
    public double AngleDecay = 1.0;

    [DataField]
    public double MaxAngle = 1.0;

    [DataField]
    public double MinAngle = 1.0;

    [DataField]
    public float FireRate = 1.0f;

    [DataField]
    public float ProjectileSpeedFlat = 0;

    [DataField]
    public float ProjectileSpeedMultiplier = 1.0f;
}
