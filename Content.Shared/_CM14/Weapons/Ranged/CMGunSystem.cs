using System.Numerics;
using Content.Shared._CM14.Weapons.Ranged.Whitelist;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Weapons.Ranged;

public sealed class CMGunSystem : EntitySystem
{
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<AmmoFixedDistanceComponent, AmmoShotEvent>(OnAmmoFixedDistanceShot);

        SubscribeLocalEvent<ProjectileFixedDistanceComponent, StartCollideEvent>(OnProjectileStop);
        SubscribeLocalEvent<ProjectileFixedDistanceComponent, ComponentRemove>(OnProjectileStop);
        SubscribeLocalEvent<ProjectileFixedDistanceComponent, PhysicsSleepEvent>(OnProjectileStop);

        SubscribeLocalEvent<GunShowUseDelayComponent, GunShotEvent>(OnShowUseDelayShot);
        SubscribeLocalEvent<GunShowUseDelayComponent, ItemWieldedEvent>(OnShowUseDelayWielded);

        SubscribeLocalEvent<GunUserWhitelistComponent, AttemptShootEvent>(OnGunUserWhitelistAttemptShoot);
    }

    private void OnAmmoFixedDistanceShot(Entity<AmmoFixedDistanceComponent> ent, ref AmmoShotEvent args)
    {
        if (!TryComp(ent, out GunComponent? gun) ||
            gun.ShootCoordinates is not { } target)
        {
            return;
        }

        var from = _transform.GetMapCoordinates(ent);
        var to = target.ToMap(EntityManager, _transform);
        if (from.MapId != to.MapId)
            return;

        var direction = to.Position - from.Position;
        if (direction == Vector2.Zero)
            return;

        var time = _timing.CurTime;
        var normalized = direction.Normalized();
        foreach (var projectile in args.FiredProjectiles)
        {
            if (!_physicsQuery.TryComp(projectile, out var physics))
                continue;

            var impulse = normalized * gun.ProjectileSpeedModified * physics.Mass;
            _physics.SetLinearVelocity(projectile, Vector2.Zero, body: physics);
            _physics.ApplyLinearImpulse(projectile, impulse, body: physics);
            _physics.SetBodyStatus(projectile, physics, BodyStatus.InAir);

            var comp = EnsureComp<ProjectileFixedDistanceComponent>(projectile);
            comp.FlyEndTime = time + TimeSpan.FromSeconds(direction.Length() / gun.ProjectileSpeedModified);
        }
    }

    private void OnProjectileStop<T>(Entity<ProjectileFixedDistanceComponent> ent, ref T args)
    {
        StopProjectile(ent);
    }

    private void OnShowUseDelayShot(Entity<GunShowUseDelayComponent> ent, ref GunShotEvent args)
    {
        UpdateDelay(ent);
    }

    private void OnShowUseDelayWielded(Entity<GunShowUseDelayComponent> ent, ref ItemWieldedEvent args)
    {
        UpdateDelay(ent);
    }

    private void OnGunUserWhitelistAttemptShoot(Entity<GunUserWhitelistComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (_whitelist.IsValid(ent.Comp.Whitelist, args.User))
            return;

        args.Cancelled = true;
        var gun = Loc.GetString("zzzz-the", ("ent", ent.Owner));
        _popup.PopupClient($"You don't seem to know how to use {gun}", args.User, args.User);
    }

    private void StopProjectile(Entity<ProjectileFixedDistanceComponent> projectile)
    {
        if (!_physicsQuery.TryGetComponent(projectile, out var physics))
            return;

        _physics.SetLinearVelocity(projectile, Vector2.Zero, body: physics);
        _physics.SetBodyStatus(projectile, physics, BodyStatus.OnGround);

        if (physics.Awake)
            _broadphase.RegenerateContacts(projectile, physics);
    }

    private void UpdateDelay(Entity<GunShowUseDelayComponent> ent)
    {
        if (!TryComp(ent, out GunComponent? gun))
            return;

        var remaining = gun.NextFire - _timing.CurTime;
        if (remaining <= TimeSpan.Zero)
            return;

        var useDelay = EnsureComp<UseDelayComponent>(ent);
        _useDelay.SetLength((ent, useDelay), remaining, ent.Comp.DelayId);
        _useDelay.TryResetDelay((ent, useDelay), false, ent.Comp.DelayId);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<ProjectileFixedDistanceComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (time < comp.FlyEndTime)
                continue;

            RemCompDeferred<ProjectileFixedDistanceComponent>(uid);
        }
    }
}
