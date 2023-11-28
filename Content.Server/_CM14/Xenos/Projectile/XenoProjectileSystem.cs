using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._CM14.Xenos.Projectile;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._CM14.Xenos.Projectile;

public sealed class XenoProjectileSystem : SharedXenoProjectileSystem
{
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;

    protected override void Shoot(EntityUid xeno, EntProtoId projectileId, float speed, MapCoordinates origin, MapCoordinates target)
    {
        base.Shoot(xeno, projectileId, speed, origin, target);

        var diff = target.Position - origin.Position;
        var xenoVelocity = _physics.GetMapLinearVelocity(xeno);
        var spit = Spawn(projectileId, origin);
        diff *= speed / diff.Length();

        _gun.ShootProjectile(spit, diff, xenoVelocity, xeno, xeno);
    }
}
