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
public record struct AttachableModifierConditions(
    bool UnwieldedOnly,
    bool WieldedOnly,
    bool ActiveOnly,
    bool InactiveOnly,
    EntityWhitelist? Whitelist
);

[DataRecord, Serializable, NetSerializable]
public record struct AttachableWeaponMeleeModifierSet(
    AttachableModifierConditions? Conditions,
    DamageSpecifier? BonusDamage
);

[DataRecord, Serializable, NetSerializable]
public record struct AttachableWeaponRangedModifierSet(
    AttachableModifierConditions? Conditions,
    FixedPoint2 AccuracyAddMult, // Not implemented yet. Added to have all the values already on our attachments, so whoever implements this doesn't need to dig through CM13. Remove this comment once implemented.
    FixedPoint2 DamageFalloffAddMult, // As above.
    int ShotsPerBurstFlat,
    FixedPoint2 DamageAddMult,
    float RecoilFlat,
    double AngleIncreaseFlat,
    double AngleDecayFlat,
    double MaxAngleFlat,
    double MinAngleFlat,
    float FireDelayFlat, // CM13 fire delay is in BYOND ticks, i.e. deciseconds. Conversion to RMC: BYOND_FIRE_DELAY / 10
    float ProjectileSpeedFlat
);

[DataRecord, Serializable, NetSerializable]
public record struct AttachableSpeedModifierSet(
    AttachableModifierConditions? Conditions,
    float Walk,
    float Sprint
);

[DataRecord, Serializable, NetSerializable]
public record struct AttachableSizeModifierSet(
    AttachableModifierConditions? Conditions,
    int SizeIncrement
);
