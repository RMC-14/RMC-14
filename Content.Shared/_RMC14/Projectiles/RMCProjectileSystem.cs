using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._RMC14.Projectiles;

public sealed class RMCProjectileSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DeleteOnCollideComponent, StartCollideEvent>(OnDeleteOnCollideStartCollide);
        SubscribeLocalEvent<ModifyTargetOnHitComponent, ProjectileHitEvent>(OnModifyTargetOnHit);
        SubscribeLocalEvent<ProjectileMaxRangeComponent, MapInitEvent>(OnProjectileMaxRangeMapInit);

        SubscribeLocalEvent<RMCProjectileDamageFalloffComponent, MapInitEvent>(OnFalloffProjectileMapInit);
        SubscribeLocalEvent<RMCProjectileDamageFalloffComponent, ProjectileHitEvent>(OnFalloffProjectileHit);

        SubscribeLocalEvent<SpawnOnTerminateComponent, MapInitEvent>(OnSpawnOnTerminatingMapInit);
        SubscribeLocalEvent<SpawnOnTerminateComponent, EntityTerminatingEvent>(OnSpawnOnTerminatingTerminate);

        SubscribeLocalEvent<PreventCollideWithDeadComponent, PreventCollideEvent>(OnPreventCollideWithDead);
    }

    private void OnDeleteOnCollideStartCollide(Entity<DeleteOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (_net.IsServer)
            QueueDel(ent);
    }

    private void OnModifyTargetOnHit(Entity<ModifyTargetOnHitComponent> ent, ref ProjectileHitEvent args)
    {
        if (!_whitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, args.Target))
            return;

        if (ent.Comp.Add is { } add)
            EntityManager.AddComponents(args.Target, add);
    }

    private void OnProjectileMaxRangeMapInit(Entity<ProjectileMaxRangeComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Origin = _transform.GetMoverCoordinates(ent);
        Dirty(ent);
    }

    private void OnFalloffProjectileMapInit(Entity<RMCProjectileDamageFalloffComponent> projectile, ref MapInitEvent args)
    {
        projectile.Comp.ShotFrom = _transform.GetMoverCoordinates(projectile.Owner);
        Dirty(projectile);
    }

    private void OnFalloffProjectileHit(Entity<RMCProjectileDamageFalloffComponent> projectile, ref ProjectileHitEvent args)
    {
        if (projectile.Comp.ShotFrom == null || projectile.Comp.MinRemainingDamageMult < 0)
            return;

        var distance = (_transform.GetMoverCoordinates(args.Target).Position - projectile.Comp.ShotFrom.Value.Position).Length();
        var minDamage = args.Damage.GetTotal() * projectile.Comp.MinRemainingDamageMult;

        foreach (var threshold in projectile.Comp.Thresholds)
        {
            var pastEffectiveRange = distance - threshold.Range;

            if (pastEffectiveRange <= 0)
                continue;

            var totalDamage = args.Damage.GetTotal();

            if (totalDamage <= minDamage)
                break;

            var extraMult = threshold.IgnoreModifiers ? 1 : projectile.Comp.WeaponMult;
            var minMult = FixedPoint2.Min(minDamage / totalDamage, 1);

            args.Damage *= FixedPoint2.Clamp((totalDamage - pastEffectiveRange * threshold.Falloff * extraMult) / totalDamage, minMult, 1);

        }
    }

    public void SetProjectileFalloffWeaponMult(Entity<RMCProjectileDamageFalloffComponent> projectile, FixedPoint2 mult)
    {
        projectile.Comp.WeaponMult = mult;
        Dirty(projectile);
    }

    private void OnSpawnOnTerminatingMapInit(Entity<SpawnOnTerminateComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Origin = _transform.GetMoverCoordinates(ent);
        Dirty(ent);
    }

    private void OnSpawnOnTerminatingTerminate(Entity<SpawnOnTerminateComponent> ent, ref EntityTerminatingEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(ent, out TransformComponent? transform))
            return;

        if (TerminatingOrDeleted(transform.ParentUid))
            return;

        var coordinates = transform.Coordinates;
        if (ent.Comp.ProjectileAdjust &&
            ent.Comp.Origin is { } origin &&
            coordinates.TryDelta(EntityManager, _transform, origin, out var delta) &&
            delta.Length() > 0)
        {
            coordinates = coordinates.Offset(delta.Normalized() / -2);
        }

        SpawnAtPosition(ent.Comp.Spawn, coordinates);

        if (ent.Comp.Popup is { } popup)
            _popup.PopupCoordinates(Loc.GetString(popup), coordinates, ent.Comp.PopupType ?? PopupType.Small);
    }

    private void OnPreventCollideWithDead(Entity<PreventCollideWithDeadComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (_mobState.IsDead(args.OtherEntity))
            args.Cancelled = true;
    }

    public void SetMaxRange(Entity<ProjectileMaxRangeComponent> ent, float max)
    {
        ent.Comp.Max = max;
        Dirty(ent);
    }

    private void StopProjectile(Entity<ProjectileMaxRangeComponent> ent)
    {
        if (ent.Comp.Delete)
        {
            if (_net.IsServer)
                QueueDel(ent);
        }
        else
        {
            _physics.SetLinearVelocity(ent, Vector2.Zero);
            RemCompDeferred<ProjectileMaxRangeComponent>(ent);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var maxQuery = EntityQueryEnumerator<ProjectileMaxRangeComponent>();
        while (maxQuery.MoveNext(out var uid, out var comp))
        {
            var coordinates = _transform.GetMoverCoordinates(uid);
            if (comp.Origin is not { } origin ||
                !coordinates.TryDistance(EntityManager, _transform, origin, out var distance))
            {
                StopProjectile((uid, comp));
                continue;
            }

            if (distance < comp.Max && Math.Abs(distance - comp.Max) > 0.1f)
                continue;

            StopProjectile((uid, comp));
        }
    }
}
