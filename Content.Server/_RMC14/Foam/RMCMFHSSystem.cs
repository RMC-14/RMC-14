using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Foam;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Atmos.Components;
using Content.Shared.Coordinates;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Foam;

public sealed class RMCMFHSSystem : EntitySystem
{
    private static readonly HashSet<string> FoamPrototypes =
    [
        "MetalFoam",
        "RMCAluminiumMetalFoamEffect",
        "RMCIronMetalFoamEffect",
        "RMCFoamedAluminiumMetal",
        "RMCFoamedIronMetal",
        "RMCMFHSFoam",
        "RMCMFHSFoamedIron",
    ];

    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _flammable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly RMCObstacleSlammingSystem _obstacleSlamming = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<EntityUid> _entities = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCMFHSComponent, RMCTriggerEvent>(OnTriggered);
        SubscribeLocalEvent<RMCMFHSPostThrowStunComponent, LandEvent>(OnKnockbackLanded);
        SubscribeLocalEvent<RMCMFHSPostThrowStunComponent, StopThrowEvent>(OnKnockbackStopped);
    }

    private void OnKnockbackLanded(Entity<RMCMFHSPostThrowStunComponent> ent, ref LandEvent args)
    {
        ApplyPostThrowStun(ent);
    }

    private void OnKnockbackStopped(Entity<RMCMFHSPostThrowStunComponent> ent, ref StopThrowEvent args)
    {
        ApplyPostThrowStun(ent);
    }

    private void ApplyPostThrowStun(Entity<RMCMFHSPostThrowStunComponent> ent)
    {
        var duration = ent.Comp.Duration;
        RemComp<RMCMFHSPostThrowStunComponent>(ent);

        // Use the two underlying effects independently. TryParalyze short-circuits if its
        // knockdown status cannot be added and can consequently skip the action stun too.
        // Explicitly forcing the standing state also guarantees the blast victim visibly
        // lands on the ground instead of merely sliding to the destination.
        _standing.Down(ent, dropHeldItems: false, force: true);
        _stun.TryStun(ent, duration, true, force: true);

        Timer.Spawn(duration, () =>
        {
            if (TerminatingOrDeleted(ent))
                return;

            // Do not stand somebody who acquired a genuine knockdown from another source
            // during the MFHS recovery window.
            if (!HasComp<KnockedDownComponent>(ent))
                _standing.Stand(ent, force: true);
        });
    }

    private void OnTriggered(Entity<RMCMFHSComponent> ent, ref RMCTriggerEvent args)
    {
        if (!_rmcMap.TryGetTileRefForEnt(ent.Owner.ToCoordinates(), out var grid, out var originTile))
            return;

        var waves = GetFootprint(grid, originTile, ent.Comp.Range);
        var origin = _map.GridTileToLocal(grid, grid, originTile.GridIndices);
        var foam = ent.Comp.Foam;

        // The entire final footprint is cleared before the first foam entity is created.
        foreach (var wave in waves)
        {
            foreach (var tile in wave)
                ClearTile(ent.Owner, origin, tile, ent.Comp);
        }

        for (var i = 0; i < waves.Count; i++)
        {
            var wave = waves[i];
            Timer.Spawn(ent.Comp.SpreadDelay * i, () =>
            {
                foreach (var tile in wave)
                {
                    TryStartFoam(tile, foam, ent.Comp);
                }
            });
        }
    }

    private List<List<EntityCoordinates>> GetFootprint(Entity<MapGridComponent> grid, TileRef origin, int range)
    {
        var result = new List<List<EntityCoordinates>>();
        var visited = new HashSet<Vector2i> { origin.GridIndices };
        var frontier = new List<Vector2i> { origin.GridIndices };

        for (var distance = 0; distance <= range && frontier.Count > 0; distance++)
        {
            var wave = new List<EntityCoordinates>();
            var next = new List<Vector2i>();

            foreach (var indices in frontier)
            {
                if (!_map.TryGetTileRef(grid, grid, indices, out var tile) || tile.Tile.IsEmpty)
                    continue;

                var coords = _map.GridTileToLocal(grid, grid, indices);
                if (IsBlocked(coords))
                    continue;

                wave.Add(coords);
                if (distance == range)
                    continue;

                foreach (var direction in _rmcMap.CardinalDirections)
                {
                    var neighbor = indices.Offset(direction);
                    if (visited.Add(neighbor))
                        next.Add(neighbor);
                }
            }

            if (wave.Count > 0)
                result.Add(wave);
            frontier = next;
        }

        return result;
    }

    private bool IsBlocked(EntityCoordinates coordinates)
    {
        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(coordinates);
        while (anchored.MoveNext(out var uid))
        {
            if (TryComp<AirtightComponent>(uid, out var airtight) && airtight.AirBlocked)
                return true;
        }

        return false;
    }

    private void ClearTile(EntityUid grenade, EntityCoordinates origin, EntityCoordinates tile, RMCMFHSComponent component)
    {
        _entities.Clear();
        _lookup.GetEntitiesInRange(tile, 0.45f, _entities, LookupFlags.Uncontained);

        var originMap = _transform.ToMapCoordinates(origin);
        foreach (var target in _entities)
        {
            if (target == grenade || TerminatingOrDeleted(target))
                continue;

            if (TryComp<FlammableComponent>(target, out var flammable) && flammable.OnFire)
                _flammable.Extinguish((target, flammable));

            if (HasComp<TileFireComponent>(target))
                QueueDel(target);

            var mob = HasComp<MobStateComponent>(target);
            var largeXeno = HasComp<XenoComponent>(target) &&
                            TryComp<RMCSizeComponent>(target, out var size) &&
                            size.Size >= RMCSizes.Big;
            if (!TryComp<PhysicsComponent>(target, out var physics) || Transform(target).Anchored)
            {
                if (mob && !largeXeno)
                {
                    _standing.Down(target, dropHeldItems: false, force: true);
                    _stun.TryStun(target, component.StunTime, true, force: true);
                    Timer.Spawn(component.StunTime, () =>
                    {
                        if (!TerminatingOrDeleted(target) && !HasComp<KnockedDownComponent>(target))
                            _standing.Stand(target, force: true);
                    });
                }

                continue;
            }

            var targetMap = _transform.GetMapCoordinates(target);
            var direction = targetMap.Position - originMap.Position;
            var distance = direction.Length();
            var knockbackOrigin = originMap;
            if (distance < 0.001f)
            {
                direction = Vector2.UnitX;
                knockbackOrigin = new MapCoordinates(targetMap.Position - direction * 0.1f, targetMap.MapId);
            }

            if (mob)
            {
                // Mob movement controllers immediately overwrite a raw velocity change. Use RMC's
                // established knockback path, while suppressing its obstacle-impact damage.
                _obstacleSlamming.MakeImmune(target);
                if (!largeXeno)
                {
                    var postThrowStun = EnsureComp<RMCMFHSPostThrowStunComponent>(target);
                    postThrowStun.Duration = component.StunTime;
                }

                _sizeStun.KnockBack(
                    target,
                    knockbackOrigin,
                    component.Range + 1f,
                    component.Range + 1f,
                    component.KnockbackSpeed,
                    ignoreSize: true);
            }
            else
            {
                // Loose objects do not have a movement controller, so a harmless velocity shove is sufficient.
                _physics.SetLinearVelocity(target, direction.Normalized() * component.KnockbackSpeed, body: physics);
            }
        }
    }

    private void TryStartFoam(EntityCoordinates tile, EntProtoId foam, RMCMFHSComponent component)
    {
        // Mobs do not prevent the expansion from starting: the footprint-wide clear has
        // already knocked them down and pushed them outward. Only a mob that remains on
        // the tile when it is ready to become dense suppresses that final wall.
        if (!CanFormFoam(tile, ignoreMobs: true))
            return;

        SpawnAtPosition(foam, tile);
        _audio.PlayPvs(component.DeploySound, tile);

        Timer.Spawn(component.SolidifyDelay, () =>
        {
            // Recheck immediately before becoming dense. A mob can enter the tile during
            // the brief expansion animation, or another foam wall can finish first.
            if (CanFormFoam(tile, ignoreExpandingFoam: true))
                SpawnAtPosition(component.SolidFoam, tile);
        });
    }

    private bool CanFormFoam(EntityCoordinates tile, bool ignoreExpandingFoam = false, bool ignoreMobs = false)
    {
        _entities.Clear();
        _lookup.GetEntitiesInRange(tile, 0.45f, _entities, LookupFlags.Uncontained);

        foreach (var target in _entities)
        {
            // Never solidify around a mob, even if the initial knockback could not move it.
            if (!ignoreMobs && HasComp<MobStateComponent>(target))
                return false;
        }

        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(tile);
        while (anchored.MoveNext(out var uid))
        {
            var prototype = MetaData(uid).EntityPrototype?.ID ?? string.Empty;
            if (ignoreExpandingFoam && prototype == "RMCMFHSFoam")
                continue;

            if (FoamPrototypes.Contains(prototype))
                return false;

            if (TryComp<AirtightComponent>(uid, out var airtight) && airtight.AirBlocked)
                return false;
        }

        return true;
    }
}

[RegisterComponent]
public sealed partial class RMCMFHSPostThrowStunComponent : Component
{
    public TimeSpan Duration = TimeSpan.FromSeconds(0.3);
}
