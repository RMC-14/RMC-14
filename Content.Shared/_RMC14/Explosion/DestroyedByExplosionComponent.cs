using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCExplosionSystem))]
public sealed partial class DestroyedByExplosionComponent : Component
{
    /// <summary>
    ///     Whether the entity can be destroyed by explosions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsExplodable = true;

    /// <summary>
    ///     If the damage is equal to or below this value, the chance to destroy the entity is the <see cref="LowIntensityDestroyChance"/> .
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 LowIntensityDamageThreshold = 200;

    /// <summary>
    ///     If the explosion damage received is above this value, the chance to destroy the entity is the <see cref="HighIntensityDestroyChance"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 HighIntensityDamageThreshold = 400;

    /// <summary>
    ///     The chance to destroy the entity if the explosion damage is below the <see cref="LowIntensityDamageThreshold"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LowIntensityDestroyChance= 0.05f;

    /// <summary>
    ///     The chance to destroy the entity if the explosion damage is above the <see cref="LowIntensityDamageThreshold"/> but below the <see cref="HighIntensityDamageThreshold"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MediumIntensityDestroyChance = 0.5f;

    /// <summary>
    ///     The chance to destroy the entity if the explosion damage is above the <see cref="HighIntensityDamageThreshold"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HighIntensityDestroyChance = 1f;
}
