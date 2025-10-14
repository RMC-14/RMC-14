using System.Runtime.InteropServices;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Explosion;

public sealed class CMClusterGrenadeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<UserLimitHitsComponent> _userLimits;

    public override void Initialize()
    {
        _userLimits = GetEntityQuery<UserLimitHitsComponent>();

        SubscribeLocalEvent<ClusterLimitHitsComponent, CMClusterSpawnedEvent>(OnClusterLimitHitsSpawned);
        SubscribeLocalEvent<ProjectileLimitHitsComponent, ProjectileHitEvent>(OnProjectileLimitHitsHit);
        SubscribeLocalEvent<ProjectileLimitHitsComponent, PreventCollideEvent>(OnProjectileLimitHitsPreventCollide);
    }

    private void OnClusterLimitHitsSpawned(Entity<ClusterLimitHitsComponent> ent, ref CMClusterSpawnedEvent args)
    {
        foreach (var spawned in args.Spawned)
        {
            var projectile = EnsureComp<ProjectileLimitHitsComponent>(spawned);

            if (ent.Comp.IgnoreFirstHit)
                projectile.IgnoredEntities = args.HitEntities;

            projectile.Limit = ent.Comp.Limit;
            projectile.OriginEntity = args.OriginEntity;
            Dirty(spawned, projectile);
        }
    }

    private void OnProjectileLimitHitsHit(Entity<ProjectileLimitHitsComponent> ent, ref ProjectileHitEvent args)
    {
        if (!_userLimits.TryComp(args.Target, out var limit))
            return;

        if (!CanHit((args.Target, limit), ent))
        {
            args.Handled = true;
            return;
        }

        limit.HitBy.Add(new Hit(GetNetEntity(ent.Comp.OriginEntity), _timing.CurTime + limit.Expire, ent.Comp.ExtraId));
        Dirty(args.Target, limit);
    }

    private void OnProjectileLimitHitsPreventCollide(Entity<ProjectileLimitHitsComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled ||
            !_userLimits.TryComp(args.OtherEntity, out var limit))
        {
            return;
        }

        if (!CanHit((args.OtherEntity, limit), ent))
            args.Cancelled = true;
    }

    public bool CanHit(Entity<UserLimitHitsComponent> user, Entity<ProjectileLimitHitsComponent> projectile)
    {
        var time = _timing.CurTime;
        var span = CollectionsMarshal.AsSpan(user.Comp.HitBy);
        var count = 0;
        foreach (ref var hit in span)
        {
            if (projectile.Comp.Limit == 0 &&
                !projectile.Comp.IgnoredEntities.Contains(user))
                continue;

            // TODO RMC14 save me from this if statement
            if (GetEntity(hit.Id) == projectile.Comp.OriginEntity &&
                (hit.ExtraId == null || hit.ExtraId == projectile.Comp.ExtraId) ||
                hit.Id == GetNetEntity(projectile.Owner) &&
                (hit.ExtraId == null || hit.ExtraId == projectile.Comp.ExtraId) &&
                hit.ExpireAt > time)
            {
                count++;
            }

            if (count >= projectile.Comp.Limit && count != 0)
                return false;
        }

        return true;
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<UserLimitHitsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var removed = false;
            for (var i = comp.HitBy.Count - 1; i >= 0; i--)
            {
                var hitBy = comp.HitBy[i];
                if (time <= hitBy.ExpireAt)
                    continue;

                comp.HitBy.RemoveSwap(i);
                removed = true;
            }

            if (removed)
                Dirty(uid, comp);
        }
    }
}
