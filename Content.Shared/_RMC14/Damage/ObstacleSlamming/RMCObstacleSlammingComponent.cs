using Content.Shared.Damage.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Damage.ObstacleSlamming;

/// <summary>
/// Damages a mob when it is thrown into an obstacle at high velocity.
/// </summary>
[Access(typeof(RMCObstacleSlammingSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCObstacleSlammingComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MinimumSpeed = 4.5f;

    /// <summary>
    /// MOB_SIZE_COEFF in CM
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MobSizeCoefficient = 20;

    /// <summary>
    /// THROW_SPEED_DENSE_COEFF in CM
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ThrowSpeedCoefficient = 0.2f;

    [DataField, AutoNetworkedField]
    public float KnockbackPower = 1;

    [DataField, AutoNetworkedField]
    public float KnockBackSpeed = 3;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundHit = new SoundCollectionSpecifier("MetalSlam");

    [DataField, AutoNetworkedField]
    public EntProtoId? HitEffect;

    [DataField, AutoNetworkedField]
    public TimeSpan DamageCooldown = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan? LastHit;
}
