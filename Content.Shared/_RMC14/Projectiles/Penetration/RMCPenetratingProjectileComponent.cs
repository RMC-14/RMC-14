using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Projectiles.Penetration;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCPenetratingProjectileComponent : Component
{
    /// <summary>
    ///     The remaining range of the projectile.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 32f;

    /// <summary>
    ///     The coordinates the projectile was shot from.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityCoordinates? ShotFrom;

    /// <summary>
    ///     The multiplier for range and damage loss if a membrane is hit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> HitTargets = new();

    /// <summary>
    ///     The amount of range lost per hit entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RangeLossPerHit = 3f;

    /// <summary>
    ///     The amount of damage lost per hit entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DamageMultiplierLossPerHit = 0.2f;

    /// <summary>
    ///     The multiplier for range and damage loss if a wall is hit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WallMultiplier = 3f;

    /// <summary>
    ///     The multiplier for range and damage loss if a big xeno is hit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BigXenoMultiplier = 2f;

    /// <summary>
    ///     The multiplier for range and damage loss if a thick membrane is hit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ThickMembraneMultiplier = 1.5f;

    /// <summary>
    ///     The multiplier for range and damage loss if a membrane is hit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MembraneMultiplier = 1f;
}
