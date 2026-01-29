using Content.Server.Explosion.EntitySystems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._RMC14.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Explosion;

public sealed class HefaKnightsExplosionSystem : SharedHefaKnightsExplosionSystem
{
    [Dependency] private readonly RMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly List<EntityUid> _spawned = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HefaHelmetComponent, TriggerEvent>(OnHelmetTrigger);
    }

    private void OnHelmetTrigger(Entity<HefaHelmetComponent> ent, ref TriggerEvent args)
    {
        ExplodeHelmet(ent, args.User ?? ent.Comp.Wearer);
        args.Handled = true;
    }

    protected override void ExplodeSword(Entity<HefaSwordSplosionComponent> ent, EntityUid user, EntityUid target)
    {
        if (!TryComp(ent, out ExplosiveComponent? explosive))
            return;

        var userXform = Transform(user);
        var targetXform = Transform(target);
        var targetCoords = TransformSystem.GetMapCoordinates(targetXform);
        var userRotation = userXform.LocalRotation;

        // Sword uses a cone pattern facing the direction the user is looking
        SpawnShrapnel(
            ent.Comp.ShrapnelPrototype,
            ent.Comp.ShrapnelCount,
            ent.Comp.SpreadAngle,
            ent.Comp.ProjectileSpeed,
            ent.Comp.MinVelocity,
            ent.Comp.MaxVelocity,
            user,
            targetCoords,
            ent,
            userRotation);

        QueueRMCExplosion(ent, explosive, targetCoords, user);
    }

    protected override void ExplodeHelmet(Entity<HefaHelmetComponent> ent, EntityUid? user)
    {
        if (!TryComp(ent, out ExplosiveComponent? explosive))
            return;

        // Get proper coordinates - handles the case where helmet is equipped (in container)
        var (origin, coords) = GetExplosionOrigin(ent);
        var shooter = ent.Comp.Wearer ?? user ?? origin;

        // Helmet uses a full circle pattern (no base rotation)
        SpawnShrapnel(
            ent.Comp.ShrapnelPrototype,
            ent.Comp.ShrapnelCount,
            ent.Comp.SpreadAngle,
            ent.Comp.ProjectileSpeed,
            ent.Comp.MinVelocity,
            ent.Comp.MaxVelocity,
            shooter,
            coords,
            ent,
            baseRotation: null);

        QueueRMCExplosion(ent, explosive, coords, shooter);
    }

    private void QueueRMCExplosion(
        EntityUid source,
        ExplosiveComponent explosive,
        MapCoordinates coords,
        EntityUid cause)
    {
        _rmcExplosion.QueueExplosion(
            coords,
            explosive.ExplosionType,
            explosive.TotalIntensity,
            explosive.IntensitySlope,
            explosive.MaxIntensity,
            cause,
            explosive.TileBreakScale,
            explosive.MaxTileBreak,
            explosive.CanCreateVacuum);

        var ev = new CMExplosiveTriggeredEvent();
        RaiseLocalEvent(source, ref ev);

        QueueDel(source);
    }

    private void SpawnShrapnel(
        string shrapnelPrototype,
        int shrapnelCount,
        float spreadAngle,
        float projectileSpeed,
        float minVelocity,
        float maxVelocity,
        EntityUid shooter,
        MapCoordinates coords,
        EntityUid sourceEntity,
        Angle? baseRotation)
    {
        if (shrapnelCount <= 0)
            return;

        // Validate coordinates
        if (coords.MapId == MapId.Nullspace)
        {
            Log.Warning($"Attempted to spawn shrapnel at invalid coordinates for entity {ToPrettyString(sourceEntity)}");
            return;
        }

        _spawned.Clear();

        var segmentAngle = spreadAngle / shrapnelCount;

        // For cone pattern (baseRotation provided): center the spread around the facing direction
        // For circle pattern (baseRotation null): start at 0 degrees
        double startAngle;
        if (baseRotation.HasValue)
        {
            // Subtract 90 degrees because sprite rotation 0 points up (+Y), but we want forward direction
            startAngle = baseRotation.Value.Degrees - 90 - spreadAngle / 2;
        }
        else
        {
            startAngle = 0;
        }

        for (var i = 0; i < shrapnelCount; i++)
        {
            var shrapnel = Spawn(shrapnelPrototype, coords);
            var angle = Angle.FromDegrees(startAngle + segmentAngle * (i + 0.5)); // Even distribution across cone.
            var direction = angle.ToVec().Normalized();
            var velocity = _random.NextVector2(minVelocity, maxVelocity);

            _gun.ShootProjectile(shrapnel, direction, velocity, shooter, shooter, projectileSpeed);
            _spawned.Add(shrapnel);
        }

        // Raise events for ClusterLimitHits and other systems
        var clusterEv = new CMClusterSpawnedEvent(_spawned, new List<EntityUid>(), shooter);
        RaiseLocalEvent(sourceEntity, ref clusterEv);
        RaiseLocalEvent(sourceEntity, new AmmoShotEvent { FiredProjectiles = _spawned });
    }
}
