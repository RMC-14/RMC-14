using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Projectiles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCProjectileSystem))]
public sealed partial class RMCProjectileDamageFalloffComponent : Component
{
    /// <summary>
    /// This lists all the thresholds and their falloff values.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<DamageFalloffThreshold> Thresholds = new()
    {
        new DamageFalloffThreshold(0f, 1, false),
        new DamageFalloffThreshold(22f, 9999, true)
    };

    /// <summary>
    /// This determines the minimum fraction of the projectile's original damage that must remain after falloff is applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MinRemainingDamageMult = 0.05f;

    /// <summary>
    /// This is the additional falloff multiplier applied by the firing weapon.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 WeaponMult = 1;

    /// <summary>
    /// These are the coordinates from which the projectile was shot. Used to determine the distance travelled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityCoordinates? ShotFrom;
}

[DataRecord, Serializable, NetSerializable]
public record struct DamageFalloffThreshold(
    /// <summary>
    /// The range at which falloff starts to take effect.
    /// Conversion from 13: effective_range_max
    /// </summary>
    float Range,

    /// <summary>
    /// This is the number by which the projectile's damage is decreased for each tile travelled beyond its effective range.
    /// Conversion from 13: damage_falloff
    /// </summary>
    FixedPoint2 Falloff,

    /// <summary>
    /// This makes this falloff value ignore the firing weapon's falloff multiplier. Used primarily to simulate having a capped maximum range. Should generally be false.
    /// </summary>
    bool IgnoreModifiers
);
