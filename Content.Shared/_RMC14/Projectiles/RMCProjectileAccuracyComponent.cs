using Content.Shared._RMC14.Random;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Projectiles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCProjectileSystem), typeof(CMGunSystem))]
public sealed partial class RMCProjectileAccuracyComponent : Component
{
    /// <summary>
    /// This lists all the thresholds and their falloff values.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<AccuracyFalloffThreshold> Thresholds = new()
    {
        new AccuracyFalloffThreshold(5f, 10, false)
    };

    /// <summary>
    /// Minimum hit chance.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MinAccuracy = 5;

    /// <summary>
    /// The accuracy of the projectile before taking into account any modifiers reliant on the target.
    /// This value is multiplied by the firing weapon's accuracy multiplier upon the projectile being shot.
    /// Conversion from 13: accuracy + accuracy (one from the ammo, the other from the projectile itself)
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Accuracy = 90;

    /// <summary>
    /// Setting this to true will make the projectile not apply the usual penalty accuracy penalty when attacking friendlies.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreFriendlyEvasion = false;

    /// <summary>
    /// If set to true, this makes the projectile automatically hit regardless of accuracy or any other modifiers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ForceHit = false;

    /// <summary>
    /// These are the coordinates from which the projectile was shot. Used to determine the distance travelled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityCoordinates? ShotFrom;

    [DataField, AutoNetworkedField]
    public long GunSeed;

    [DataField, AutoNetworkedField]
    public uint Tick;
}

[DataRecord, Serializable, NetSerializable]
public record struct AccuracyFalloffThreshold(
    /// <summary>
    /// The range at which accuracy falloff starts to take effect.
    /// Conversion from 13: accurate_range
    /// Conversion from 13 for buildup: accurate_range_min
    /// </summary>
    float Range,

    /// <summary>
    /// This is the number by which the projectile's accuracy is decreased for each tile travelled beyond its effective range.
    /// Conversion from 13: 10
    /// Conversion from 13 for buildup: accuracy_range_falloff
    /// </summary>
    FixedPoint2 Falloff,

    /// <summary>
    /// Setting this to true makes it so AccurateRange is treated as the minimum accurate range.
    /// Falloff is applied by how much the shot falls short of that distance, instead of by how much it exceeds it.
    /// </summary>
    bool Buildup
);

public enum AccuracyModifiers : int
{
    TargetOccluded = -15
}
