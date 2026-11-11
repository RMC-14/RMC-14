using System.Numerics;
using Content.Server._RMC14.Scorch;
using Content.Server._RMC14.Smoke;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Chemistry.Effects.Positive;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Chemistry;

public sealed class RMCChemVolatilesSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedRMCFlamerSystem _flamer = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _flammable = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly RMCSmokeSystem _smoke = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId ChemFireProto = "RMCTileFireGenerated";
    private static readonly EntProtoId PhosphorusSmoke = "RMCSmokePhosphorus";
    private static readonly EntProtoId Shrapnel = "CMProjectileShrapnel";
    private static readonly ProtoId<ReagentPrototype> Phosphorus = "RMCPhosphorus";
    private static readonly ProtoId<ReagentPrototype> Iron = "RMCIron";
    private static readonly ProtoId<ReagentPrototype> Phoron = "RMCPhoron";
    private static readonly ProtoId<ReagentPrototype> PolytrinicAcid = "RMCPolytrinicAcid";
    private static readonly ProtoId<ReagentPrototype> NeurotoxinPlasma = "RMCNeurotoxinPlasma";
    private static readonly ProtoId<ExplosionPrototype> ExplosionType = "RMC";

    private static readonly RMCChemVolatilesComponent DefaultVolatiles = new();

    private readonly Dictionary<(EntityUid, Vector2i), TimeSpan> _chemExploded = new();
    private readonly List<(EntityUid, Vector2i)> _expiredChemExploded = new();
    private readonly HashSet<Entity<MobStateComponent>> _epicenterMobs = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ExecuteEntityEffectEvent<RMCVolatileReactionEffect>>(OnVolatileReaction);
    }

    private void OnVolatileReaction(ref ExecuteEntityEffectEvent<RMCVolatileReactionEffect> args)
    {
        if (args.Args is not EntityEffectReagentArgs { Source: { } solution } reagentArgs)
            return;

        if (!_rmcReagent.TryIndex(args.Effect.Reagent, out var reagent) ||
            reagent.Overdose is not { } overdose ||
            reagentArgs.Quantity * args.Effect.Amount <= overdose ||
            !IsVolatileOrFireSupplement(reagent))
        {
            return;
        }

        var solutionUid = reagentArgs.TargetEntity;
        var owner = TryComp(solutionUid, out ContainedSolutionComponent? contained) ? contained.Container : solutionUid;

        if (HasComp<MobStateComponent>(owner))
            return;

        if (!HandleVolatiles(owner, solution, null))
            return;

        if (TryComp(solutionUid, out SolutionComponent? solutionComp))
            _solution.RemoveAllSolution((solutionUid, solutionComp));
    }

    private bool IsVolatileOrFireSupplement(ReagentPrototype reagent)
    {
        foreach (var property in _rmcReagent.GetProperties(reagent.ID))
        {
            if (property.IsVolatile || property.IsFireSupplement)
                return true;
        }

        return false;
    }

    /// <returns>True if a fire or an explosion was started.</returns>
    public bool HandleVolatiles(
        EntityUid owner,
        Solution solution,
        EntityUid? cause,
        float structuralExplosiveScale = 1f,
        bool triggerStructuralExplosive = false)
    {
        var coordinates = Transform(owner).Coordinates;
        var tileKey = GetTileKey(coordinates);
        var time = _timing.CurTime;
        if (IsChemExploded(tileKey, time))
            return false;

        var volatiles = CompOrNull<RMCChemVolatilesComponent>(owner) ?? DefaultVolatiles;
        var handled = false;

        var aggregate = _flamer.GetAggregateFireStats(solution);
        if (aggregate.Intensity >= 1)
        {
            var radius = Math.Clamp((int) aggregate.Radius, volatiles.MinFireRadius, volatiles.MaxFireRadius);
            var intensity = Math.Clamp((int) aggregate.Intensity, volatiles.MinFireIntensity, volatiles.MaxFireIntensity);
            var duration = Math.Clamp((int) aggregate.Duration, volatiles.MinFireDuration, volatiles.MaxFireDuration);

            // TODO RMC14: cm13 combust() switches to a star (8 lines of floor(1.5 * radius)) above 30 intensity, only reachable
            // with custom casings higher max_fire_int
            _flammable.SpawnFireDiamond(ChemFireProto, coordinates, radius, intensity, duration, aggregate.FirePenetrating, aggregate.FireColor);
            handled = true;

            var phosphorus = solution.GetTotalPrototypeQuantity(Phosphorus);
            if (phosphorus > FixedPoint2.Zero)
            {
                var smokeRadius = Math.Clamp((int) (phosphorus.Float() / volatiles.PhosphorusPerSmokeRadius), 1, volatiles.MaxFireRadius - 1);
                _smoke.SpawnEvenSmoke(PhosphorusSmoke, coordinates, smokeRadius);
            }
        }

        var explosivePower = 0f;
        var explosiveFalloff = volatiles.BaseExplosiveFalloff;
        foreach (var quantity in solution.Contents)
        {
            if (!_rmcReagent.TryIndex(quantity.Reagent.Prototype, out var reagent))
                continue;

            var power = reagent.ExplosivePower;
            var falloff = reagent.ExplosiveFalloff;
            if (_rmcReagent.TryGetProperty<Explosive>(quantity.Reagent.Prototype, out var explosive))
            {
                power += explosive.PowerDelta;
                falloff += explosive.FalloffDelta;
            }

            var volume = quantity.Quantity.Float();
            explosivePower += power * volume;
            explosiveFalloff += falloff * volume;
        }

        var exploded = false;
        if (triggerStructuralExplosive && TryComp(owner, out ExplosiveComponent? structuralExplosive))
        {
            _explosion.TriggerExplosive(owner, structuralExplosive, false, structuralExplosive.TotalIntensity * structuralExplosiveScale, user: cause);
            exploded = true;
        }

        if (explosivePower > 0)
        {
            explosivePower = MathF.Min(explosivePower, volatiles.MaxExplosivePower);
            explosiveFalloff = MathF.Max(explosiveFalloff, volatiles.MinExplosiveFalloff);

            EnsureComp<CMExplosionEffectComponent>(owner);
            EnsureComp<RMCScorchEffectComponent>(owner);

            var maxIntensity = explosivePower / volatiles.PowerPerIntensity;
            var slope = explosiveFalloff / volatiles.PowerPerIntensity;
            var totalIntensity = _explosion.RadiusToIntensity(maxIntensity / slope, slope, maxIntensity);
            var mapCoordinates = _transform.ToMapCoordinates(coordinates);
            _explosion.QueueExplosion(mapCoordinates, ExplosionType, totalIntensity, slope, maxIntensity, cause);

            SpawnShrapnel(owner, coordinates, solution, volatiles);

            var ev = new CMExplosiveTriggeredEvent();
            RaiseLocalEvent(owner, ref ev);
            exploded = true;
        }

        if (exploded)
        {
            _chemExploded[tileKey] = time + volatiles.ExplodedCooldown;
            handled = true;
        }

        return handled;
    }

    private void SpawnShrapnel(EntityUid owner, EntityCoordinates coordinates, Solution solution, RMCChemVolatilesComponent volatiles)
    {
        if (solution.GetTotalPrototypeQuantity(Phoron) >= volatiles.SpecialShardThreshold ||
            solution.GetTotalPrototypeQuantity(PolytrinicAcid) >= volatiles.SpecialShardThreshold ||
            solution.GetTotalPrototypeQuantity(NeurotoxinPlasma) >= volatiles.SpecialShardThreshold)
        {
            return;
        }

        var shards = Math.Min(volatiles.BaseShards + (int) MathF.Floor(solution.GetTotalPrototypeQuantity(Iron).Float()), volatiles.MaxShards);

        EntityUid? standing = null;
        EntityUid? lying = null;
        _epicenterMobs.Clear();
        _lookup.GetEntitiesInRange(coordinates, 0.5f, _epicenterMobs);
        foreach (var mob in _epicenterMobs)
        {
            if (!_standing.IsDown(mob))
                standing ??= mob.Owner;
            else
                lying ??= mob.Owner;
        }

        DamageSpecifier? directDamage = null;
        var armorPiercing = 0;
        if (_prototypes.TryIndex(Shrapnel, out var shrapnelProto))
        {
            if (shrapnelProto.TryGetComponent(out ProjectileComponent? projectile, _compFactory))
                directDamage = projectile.Damage;

            if (shrapnelProto.TryGetComponent(out CMArmorPiercingComponent? piercing, _compFactory))
                armorPiercing = piercing.Amount;
        }

        var increment = 360f / shards;
        for (var i = 0; i < shards; i++)
        {
            var directTarget = standing != null && _random.Prob(volatiles.ShardDirectHitChance)
                ? standing
                : lying != null && _random.Prob(volatiles.ShardDirectHitChance)
                    ? lying
                    : null;

            if (directTarget != null && directDamage != null)
            {
                _damageable.TryChangeDamage(directTarget, directDamage, armorPiercing: armorPiercing);
                continue;
            }

            var angle = Angle.FromDegrees(i * increment + _random.NextFloat(-increment / 2, increment / 2));
            var shard = Spawn(Shrapnel, coordinates);
            _gun.ShootProjectile(shard, angle.ToVec(), Vector2.Zero, owner, standing, volatiles.ShrapnelSpeed);
        }
    }

    private (EntityUid, Vector2i) GetTileKey(EntityCoordinates coordinates)
    {
        if (_transform.GetGrid(coordinates) is { } grid && TryComp(grid, out MapGridComponent? gridComp))
            return (grid, _map.CoordinatesToTile(grid, gridComp, coordinates));

        return (coordinates.EntityId, coordinates.Position.Floored());
    }

    private bool IsChemExploded((EntityUid, Vector2i) tileKey, TimeSpan time)
    {
        _expiredChemExploded.Clear();
        foreach (var (key, until) in _chemExploded)
        {
            if (time >= until)
                _expiredChemExploded.Add(key);
        }

        foreach (var key in _expiredChemExploded)
        {
            _chemExploded.Remove(key);
        }

        return _chemExploded.ContainsKey(tileKey);
    }
}
