using System.Numerics;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Stamina;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Damage;
using Content.Shared.Explosion.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Explosion;

public sealed class RMCProjectileGrenadeSystem : EntitySystem
{
    private readonly List<EntityUid> _hitEntities = new();

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RMCStaminaSystem _stamina = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileGrenadeComponent, ProjectileHitEvent>(OnStartCollide);
        SubscribeLocalEvent<ProjectileGrenadeComponent, PrepareFragmentIntoProjectilesEvent>(OnPrepareFragmentIntoProjectiles);
        SubscribeLocalEvent<ProjectileGrenadeComponent, FragmentIntoProjectilesEvent>(OnFragmentIntoProjectiles);
    }

    /// <summary>
    /// Reverses the payload shooting direction if the projectile grenade collides with an entity
    /// </summary>
    private void OnStartCollide(Entity<ProjectileGrenadeComponent> ent, ref ProjectileHitEvent args)
    {
        if (!ent.Comp.Rebounds)
            return;

        var reboundTimer = EnsureComp<ActiveTimerTriggerComponent>(ent);
        reboundTimer.TimeRemaining = ent.Comp.ReboundTimer;

        var ev = new ActiveTimerTriggerEvent(ent, args.Shooter);
        RaiseLocalEvent(ent, ref ev);
    }

    /// <summary>
    /// Overwrites the logic of the upstream <seealso cref="ProjectileGrenadeSystem"/> to allow more customization
    /// </summary>
    private void OnFragmentIntoProjectiles(Entity<ProjectileGrenadeComponent> ent, ref FragmentIntoProjectilesEvent args)
    {
        if (ent.Comp.DirectHit && ent.Comp.DirectHitChance == null && args.ShootCount == 0)
        {
            _hitEntities.Clear();
            var directHit = DirectHitLegacy(ent, args.ContentUid, args.TotalCount);
            if (directHit != null)
            {
                args.HitEntities = _hitEntities;
                args.TotalCount = directHit.Value;
            }
        }

        args.Handled = true;
        var segmentAngle = ent.Comp.SpreadAngle / args.TotalCount;
        var projectileRotation = _transform.GetMoverCoordinateRotation(ent.Owner, Transform(ent.Owner)).worldRot.Degrees + ent.Comp.DirectionAngle;

        // Give the same IFF faction and enabled state to the projectiles shot from the grenade
        if (ent.Comp.InheritIFF)
        {
            if (TryComp(ent.Owner, out ProjectileIFFComponent? grenadeIFFComponent))
            {
                _gunIFF.GiveAmmoMultiFactionIFF(args.ContentUid, grenadeIFFComponent.Factions, grenadeIFFComponent.Enabled);
            }
        }

        var angleMin = projectileRotation - ent.Comp.SpreadAngle / 2 + segmentAngle * args.ShootCount;
        var angleMax = projectileRotation - ent.Comp.SpreadAngle / 2 + segmentAngle * (args.ShootCount + 1);

        if (ent.Comp.EvenSpread)
            args.Angle = Angle.FromDegrees((angleMin + angleMax) / 2);
        else
            args.Angle = Angle.FromDegrees(_random.Next((int)angleMin, (int)angleMax));
    }

    private void OnPrepareFragmentIntoProjectiles(
        Entity<ProjectileGrenadeComponent> ent,
        ref PrepareFragmentIntoProjectilesEvent args)
    {
        if (!ent.Comp.DirectHit || ent.Comp.DirectHitChance is not { } directHitChance)
            return;

        _hitEntities.Clear();
        DirectHit(
            ent,
            args.ContentUid,
            args.TotalCount,
            args.SpawnCoordinates,
            args.User,
            directHitChance,
            args.ConsumedProjectileIndices);
        args.HitEntities = _hitEntities;
    }

    // Directly hit any entities close enough to the grenade.
    private void DirectHit(
        Entity<ProjectileGrenadeComponent> ent,
        EntityUid payloadUid,
        int projectileCount,
        MapCoordinates spawnCoordinates,
        EntityUid? user,
        float directHitChance,
        HashSet<int> consumedProjectileIndices)
    {
        if (!TryComp(payloadUid, out ProjectileComponent? projectile))
            return;

        var armorPiercing = 0;
        if (TryComp(payloadUid, out CMArmorPiercingComponent? armorPiercingComp))
            armorPiercing = armorPiercingComp.Amount;

        var nearbyEntities = _entityLookup.GetEntitiesInRange<MobStateComponent>(spawnCoordinates, MathF.Sqrt(0.5f));

        EntityUid? standingTarget = null;
        EntityUid? downedTarget = null;
        foreach (var entity in nearbyEntities)
        {
            var delta = _transform.GetMapCoordinates(entity).Position - spawnCoordinates.Position;
            if (MathF.Abs(delta.X) > 0.5f ||
                MathF.Abs(delta.Y) > 0.5f ||
                _mobState.IsDead(entity))
            {
                continue;
            }

            if (_standing.IsDown(entity))
                downedTarget ??= entity;
            else
                standingTarget ??= entity;
        }

        // CM13 treats the standing mob on the fragmentation tile as the shrapnel source and
        // prevents all of the remaining projectiles from colliding with it. RMC14
        if (standingTarget is { } fragmentationSource)
            IgnoreRemainingProjectiles(ent, fragmentationSource);

        var directHitTarget = standingTarget ?? downedTarget;
        if (directHitTarget == null)
            return;

        directHitChance = Math.Clamp(directHitChance, 0, 1);
        for (var i = 0; i < projectileCount; i++)
        {
            if (!_random.Prob(directHitChance))
                continue;

            consumedProjectileIndices.Add(i);
            ApplyDirectHit(ent, directHitTarget.Value, payloadUid, projectile, user, true, armorPiercing);
        }
    }

    private int? DirectHitLegacy(
        Entity<ProjectileGrenadeComponent> ent,
        EntityUid payloadUid,
        int projectileCount)
    {
        if (!TryComp(payloadUid, out ProjectileComponent? projectile))
            return null;

        var nearbyEntities = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(ent).Coordinates, 0.5f);
        var armorPiercing = 0;
        if (TryComp(payloadUid, out CMArmorPiercingComponent? armorPiercingComp))
            armorPiercing = armorPiercingComp.Amount;

        foreach (var entity in nearbyEntities)
        {
            if (_mobState.IsDead(entity))
                continue;

            var hitCount = Math.Min(ent.Comp.DirectHitProjectiles, projectileCount);
            for (var i = 0; i < hitCount; i++)
                ApplyDirectHit(ent, entity, payloadUid, projectile, null, armorPiercing: armorPiercing);
            projectileCount -= hitCount;

            // Make sure the leftover projectiles don't hit the entity that was hit directly
            if (!TryComp(entity, out UserLimitHitsComponent? limit))
                continue;

            _hitEntities.Add(entity);
            limit.HitBy.Add(new Hit(ent.Owner.Id, _timing.CurTime + limit.Expire, null));
            Dirty(entity, limit);

            if (projectileCount == 0)
                break;
        }

        return projectileCount;
    }

    private void ApplyDirectHit(
        Entity<ProjectileGrenadeComponent> grenade,
        EntityUid target,
        EntityUid payloadUid,
        ProjectileComponent projectile,
        EntityUid? user,
        bool attribute = false,
        int armorPiercing = 0)
    {
        var minDamageMultiplier = MathF.Max(0, MathF.Min(
            grenade.Comp.MinProjectileDamageMultiplier,
            grenade.Comp.MaxProjectileDamageMultiplier));
        var maxDamageMultiplier = MathF.Max(0, MathF.Max(
            grenade.Comp.MinProjectileDamageMultiplier,
            grenade.Comp.MaxProjectileDamageMultiplier));
        var damageMultiplier = minDamageMultiplier;
        if (minDamageMultiplier != maxDamageMultiplier)
            damageMultiplier = _random.NextFloat(minDamageMultiplier, maxDamageMultiplier);
        var damage = projectile.Damage * damageMultiplier;
        // CMArmorSystem also reads armor piercing from an attributed projectile tool. Keep the
        // explicit value for legacy direct hits, which do not provide the tool. RMC14
        var explicitArmorPiercing = attribute ? 0 : armorPiercing;
        _damage.TryChangeDamage(
            target,
            damage,
            origin: attribute ? user : null,
            tool: attribute ? payloadUid : null,
            armorPiercing: explicitArmorPiercing);

        if (attribute &&
            TryComp(payloadUid, out RMCStaminaDamageOnCollideComponent? staminaDamage) &&
            TryComp(target, out RMCStaminaComponent? stamina))
        {
            _stamina.DoStaminaDamage((target, stamina), staminaDamage.Damage);
        }
    }

    private void IgnoreRemainingProjectiles(Entity<ProjectileGrenadeComponent> grenade, EntityUid target)
    {
        _hitEntities.Add(target);
        if (!TryComp(target, out UserLimitHitsComponent? limit))
            return;

        limit.HitBy.Add(new Hit(grenade.Owner.Id, _timing.CurTime + limit.Expire, null));
        Dirty(target, limit);
    }

    public override void Update(float frametime)
    {
        var query = EntityQueryEnumerator<ProjectileGrenadeComponent, PhysicsComponent, ProjectileComponent>();
        while (query.MoveNext(out var projectileUid, out _, out var physics, out _))
        {
            _transform.SetWorldRotationNoLerp(projectileUid, physics.LinearVelocity.ToWorldAngle());
        }
    }
}

/// <summary>
///     Raised when a projectile grenade is being triggered
/// </summary>
[ByRefEvent]
public record struct FragmentIntoProjectilesEvent(EntityUid ContentUid, int TotalCount, Angle Angle, int ShootCount, List<EntityUid> HitEntities, bool Handled = false);

/// <summary>
///     Raised once before a projectile grenade starts firing its payload.
/// </summary>
[ByRefEvent]
public record struct PrepareFragmentIntoProjectilesEvent(
    EntityUid ContentUid,
    int TotalCount,
    MapCoordinates SpawnCoordinates,
    EntityUid? User)
{
    public HashSet<int> ConsumedProjectileIndices = new();
    public List<EntityUid> HitEntities = new();
}
