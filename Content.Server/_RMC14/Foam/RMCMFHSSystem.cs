using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Foam;
using Content.Shared._RMC14.Map;
using Content.Shared.Atmos.Components;
using Content.Shared.Coordinates;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Foam;

public sealed class RMCMFHSSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _flammable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<EntityUid> _entities = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCMFHSComponent, RMCTriggerEvent>(OnTriggered);
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
                    SpawnAtPosition(foam, tile);
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

            if (HasComp<MobStateComponent>(target))
                _stun.TryStun(target, component.StunTime, true);

            if (!TryComp<PhysicsComponent>(target, out _) || Transform(target).Anchored)
                continue;

            var direction = _transform.GetMapCoordinates(target).Position - originMap.Position;
            var distance = direction.Length();
            if (distance < 0.001f)
                direction = Vector2.UnitX;
            var knockback = MathF.Max(component.Knockback, component.Range + 1f - distance);
            _throwing.TryThrow(target, direction.Normalized() * knockback, component.KnockbackSpeed,
                animated: false, playSound: false, compensateFriction: true);
        }
    }
}
