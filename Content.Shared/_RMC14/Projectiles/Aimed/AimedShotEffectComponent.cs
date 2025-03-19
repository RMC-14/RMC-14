using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Projectiles.Aimed;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState()]
public sealed partial class AimedShotEffectComponent : Component
{
    /// <summary>
    ///     The amount of times the base projectile damage should be repeated on the hit target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ExtraHits;

    /// <summary>
    ///     The amount of fire stacks to apply on the hit target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int FireStacksOnHit;

    /// <summary>
    ///     The duration of the blind on the hit target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BlindDuration;

    /// <summary>
    ///     The duration of the slow on the hit target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SlowDuration;

    /// <summary>
    ///     The duration of the super slow on the hit target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SuperSlowDuration;

    /// <summary>
    ///     The current health damage to apply to the hit target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier CurrentHealthDamage = new ();
}
