using Content.Shared._RMC14.Projectiles;

namespace Content.Shared._RMC14.Weapons.Ranged;

/// <summary>
///     Projectiles with this component are bullets.
/// </summary>
[RegisterComponent]
[Access(typeof(RMCProjectileSystem), typeof(CMGunSystem))]
public sealed partial class RMCBulletComponent : Component
{
}
