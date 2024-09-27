using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Projectile;

public sealed class XenoProjectileSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private EntityQuery<ProjectileComponent> _projectileQuery;
    private EntityQuery<XenoComponent> _xenoQuery;

    public override void Initialize()
    {
        _projectileQuery = GetEntityQuery<ProjectileComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();

        SubscribeLocalEvent<XenoProjectileComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<XenoProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<XenoProjectileComponent, CMClusterSpawnedEvent>(OnClusterSpawned);
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

    private void OnProjectileHit(Entity<XenoProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (_net.IsClient && !IsClientSide(ent))
            return;

        if (ent.Comp.Hive is { } hive && _xeno.FromHive(args.Target, hive))
        {
            args.Handled = true;
            QueueDel(ent);
            return;
        }

        if (_projectileQuery.TryComp(ent, out var projectile) &&
            projectile.Shooter is { } shooter)
        {
            var ev = new XenoProjectileHitUserEvent();
            RaiseLocalEvent(shooter, ref ev);
        }
    }

    private void OnClusterSpawned(Entity<XenoProjectileComponent> ent, ref CMClusterSpawnedEvent args)
    {
        foreach (var spawned in args.Spawned)
        {
            var projectile = EnsureComp<XenoProjectileComponent>(spawned);
            projectile.Hive = ent.Comp.Hive;
            Dirty(spawned, projectile);
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
        float speed,
        float? fixedDistance = null,
        EntityUid? target = null)
    {
        var origin = _transform.GetMapCoordinates(xeno);
        var targetMap = _transform.ToMapCoordinates(targetCoords);

        if (origin.MapId != targetMap.MapId ||
            origin.Position == targetMap.Position)
        {
            return false;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno, plasma))
            return false;

        _audio.PlayPredicted(sound, xeno, xeno);
        if (_net.IsClient)
            return true;

        var originalDiff = targetMap.Position - origin.Position;
        for (var i = 0; i < shots; i++)
        {
            var projTarget = targetMap;
            if (deviation != Angle.Zero)
            {
                var angle = _random.NextAngle(-deviation / 2, deviation / 2);
                projTarget = new MapCoordinates(origin.Position + angle.RotateVec(originalDiff), targetMap.MapId);
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

            if (fixedDistance != null)
            {
                var fixedDistanceComp = EnsureComp<ProjectileFixedDistanceComponent>(projectile);
                fixedDistanceComp.FlyEndTime = _timing.CurTime + TimeSpan.FromSeconds(fixedDistance.Value / speed);
                Dirty(projectile, fixedDistanceComp);
            }

            if (target != null)
            {
                var targeted = EnsureComp<TargetedProjectileComponent>(projectile);
                targeted.Target = target.Value;
                Dirty(projectile, targeted);
            }
        }

        return true;
    }

    public bool TryShootAt(
        EntityUid xeno,
        EntityUid? target,
        EntityCoordinates? targetCoords,
        FixedPoint2 plasma,
        EntProtoId projectileId,
        SoundSpecifier? sound,
        int shots,
        Angle deviation,
        float speed,
        float? fixedDistance = null)
    {
        if (target is { Valid: true })
        {
            if (_mobState.IsDead(target.Value))
            {
                targetCoords = _transform.GetMoverCoordinates(target.Value);
            }
            else
            {
                return TryShoot(
                    xeno,
                    _transform.GetMoverCoordinates(target.Value),
                    plasma,
                    projectileId,
                    sound,
                    shots,
                    deviation,
                    speed,
                    fixedDistance,
                    target
                );
            }
        }

        if (targetCoords != null && targetCoords.Value.IsValid(EntityManager))
        {
            return TryShoot(
                xeno,
                targetCoords.Value,
                plasma,
                projectileId,
                sound,
                shots,
                deviation,
                speed,
                fixedDistance,
                target
            );
        }

        return false;
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
