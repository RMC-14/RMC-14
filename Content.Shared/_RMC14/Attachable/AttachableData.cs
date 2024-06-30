using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization;


namespace Content.Shared._RMC14.Attachable;

[DataRecord, Serializable, NetSerializable]
public record struct AttachableSlot(
    bool Locked,
    EntityWhitelist Whitelist
);

[DataRecord, Serializable, NetSerializable]
public record struct AttachableWeaponMeleeModifierSet(
    DamageSpecifier? BonusDamage
);

[DataRecord, Serializable, NetSerializable]
public record struct AttachableWeaponRangedModifierSet(
    int ShotsPerBurstFlat,
    FixedPoint2 DamageAddMult,
    float RecoilFlat,
    double AngleIncreaseFlat,
    double AngleDecayFlat,
    double MaxAngleFlat,
    double MinAngleFlat,
    float FireRateFlat,
    float ProjectileSpeedFlat
);

[DataRecord, Serializable, NetSerializable]
public record struct AttachableSpeedModifierSet(
    float Walk,
    float Sprint
);
