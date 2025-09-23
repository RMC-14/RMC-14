using Content.Shared._RMC14.Attachable.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Attachable;

[DataDefinition]
[Serializable, NetSerializable]
public partial struct AttachableSlot()
{
    [DataField]
    public bool Locked;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntProtoId<AttachableComponent>? StartingAttachable;

    [DataField]
    public List<EntProtoId<AttachableComponent>>? Random;

    [DataField]
    public float RandomChance = 1f;
}

[DataRecord, Serializable, NetSerializable]
public record struct AttachableModifierConditions(
    bool UnwieldedOnly,
    bool WieldedOnly,
    bool ActiveOnly,
    bool InactiveOnly,
    EntityWhitelist? Whitelist,
    EntityWhitelist? Blacklist
);

[DataRecord, Serializable, NetSerializable]
public record struct AttachableWeaponMeleeModifierSet(
    AttachableModifierConditions? Conditions,
    DamageSpecifier? BonusDamage
);

[DataRecord, Serializable, NetSerializable]
public record struct AttachableWeaponRangedModifierSet(
    AttachableModifierConditions? Conditions,
    FixedPoint2 AccuracyAddMult, // Affects the accuracy of all shots fired by the weapon. Conversion from 13: accuracy_mod or accuracy_unwielded_mod
    FixedPoint2 DamageFalloffAddMult, // This affects the damage falloff of all shots fired by the weapon. Conversion to RMC: damage_falloff_mod
    double BurstScatterAddMult, // This affects scatter during burst and full-auto fire. Conversion to RMC: burst_scatter_mod
    int ShotsPerBurstFlat, // Modifies the maximum number of shots in a burst.
    FixedPoint2 DamageAddMult, // Additive multiplier to damage.
    float RecoilFlat, // How much the camera shakes when you shoot.
    double ScatterFlat, // Scatter in degrees. This is how far bullets go from where you aim. Conversion to RMC: CM_SCATTER * 2
    float FireDelayFlat, // The delay between each shot. Conversion to RMC: CM_FIRE_DELAY / 10
    float ProjectileSpeedFlat, // How fast the projectiles move. Conversion to RMC: CM_PROJECTILE_SPEED * 10
    float RangeFlat // The distance in tiles at which the damage of the projectiles starts to drop off. Conversion to RMC: projectile_max_range_mod
);

[DataRecord, Serializable, NetSerializable]
public record struct AttachableWeaponFireModesModifierSet(
    AttachableModifierConditions? Conditions,
    SelectiveFire ExtraFireModes,
    SelectiveFire SetFireMode
);

// SS13 has move delay instead of speed. Move delay isn't implemented here, and approximating it through maths like fire delay is scuffed because of how the events used to change speed work.
// So instead we take the default speed values and use them to convert it to a multiplier beforehand.
// Converting from move delay to additive multiplier: 1 / (1 / SS14_SPEED + SS13_MOVE_DELAY / 10) / SS14_SPEED - 1
// Speed and move delay are inversely proportional. So 1 divided by speed is move delay and vice versa.
// We then add the ss13 move delay, and divide 1 by the result to convert it back into speed.
// Then we divide it by the original speed and subtract 1 from the result to get the additive multiplier.
[DataRecord, Serializable, NetSerializable]
public record struct AttachableSpeedModifierSet(
    AttachableModifierConditions? Conditions,
    float Walk, // Default human walk speed: 2.5f
    float Sprint // Default human sprint speed: 4.5f
);

[DataRecord, Serializable, NetSerializable]
public record struct AttachableSizeModifierSet(
    AttachableModifierConditions? Conditions,
    int Size
);

[DataRecord, Serializable, NetSerializable]
public record struct AttachableWieldDelayModifierSet(
    AttachableModifierConditions? Conditions,
    TimeSpan Delay
);
