namespace Content.Shared._RMC14.Weapons.Ranged;

/// <summary>
/// Deletes the projectile client-side when it reaches its fixed-distance stop.
/// This avoids predicted projectiles hanging around when there's no collision.
/// </summary>
[RegisterComponent]
public sealed partial class DeleteOnFixedDistanceStopComponent : Component
{
}
