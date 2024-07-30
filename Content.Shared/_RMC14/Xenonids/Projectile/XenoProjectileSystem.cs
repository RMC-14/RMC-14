using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Xenonids.Projectile;

public sealed class XenoProjectileSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private EntityQuery<XenoComponent> _xenoQuery;

    public override void Initialize()
    {
        _xenoQuery = GetEntityQuery<XenoComponent>();

        SubscribeLocalEvent<XenoProjectileComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<XenoProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(Entity<XenoProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (TryComp(args.Target, out XenoComponent? targetXeno) &&
            targetXeno.Hive == ent.Comp.Hive)
        {
            args.Handled = true;
            QueueDel(ent);
        }
    }

    private void OnPreventCollide(Entity<XenoProjectileComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled || ent.Comp.DeleteOnFriendlyXeno)
            return;

        if (_xenoQuery.TryComp(args.OtherEntity, out var xeno) &&
            xeno.Hive == ent.Comp.Hive)
        {
            args.Cancelled = true;
        }
    }

    public bool SameHive(Entity<XenoProjectileComponent?> projectile, Entity<XenoComponent?> xeno)
    {
        if (!Resolve(projectile, ref projectile.Comp, false) ||
            !Resolve(xeno, ref xeno.Comp, false))
        {
            return false;
        }

        return projectile.Comp.Hive == xeno.Comp.Hive;
    }

    public bool TryShoot(
        EntityUid xeno,
        EntityCoordinates targetCoords,
        FixedPoint2 plasma,
        EntProtoId projectileId,
        SoundSpecifier? sound,
        int shots,
        Angle deviation,
        float speed)
    {
        var origin = _transform.GetMapCoordinates(xeno);
        var target = _transform.ToMapCoordinates(targetCoords);

        if (origin.MapId != target.MapId ||
            origin.Position == target.Position)
        {
            return false;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno, plasma))
            return false;

        _audio.PlayPredicted(sound, xeno, xeno);
        if (_net.IsClient)
            return true;

        var originalDiff = target.Position - origin.Position;
        for (var i = 0; i < shots; i++)
        {
            var projTarget = target;
            if (deviation != Angle.Zero)
            {
                var angle = _random.NextAngle(-deviation / 2, deviation / 2);
                projTarget = new MapCoordinates(origin.Position + angle.RotateVec(originalDiff), target.MapId);
            }

            var diff = projTarget.Position - origin.Position;
            var xenoVelocity = _physics.GetMapLinearVelocity(xeno);
            var projectile = Spawn(projectileId, origin);
            diff *= speed / diff.Length();

            _gun.ShootProjectile(projectile, diff, xenoVelocity, xeno, xeno, speed);

            var comp = EnsureComp<XenoProjectileComponent>(projectile);
            if (TryComp(xeno, out XenoComponent? xenoComp))
            {
                comp.Hive = xenoComp.Hive;
                Dirty(projectile, comp);
            }
        }

        return true;
    }

    public void SetSameHive(Entity<XenoProjectileComponent?> projectile, Entity<XenoComponent?> xeno)
    {
        if (!Resolve(projectile, ref projectile.Comp, false) ||
            !Resolve(xeno, ref xeno.Comp, false))
        {
            return;
        }

        projectile.Comp.Hive = xeno.Comp.Hive;
        Dirty(projectile);
    }
}
