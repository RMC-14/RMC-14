using System.Numerics;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.OnCollide;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Spray;

public sealed class XenoSprayAcidSystem : EntitySystem
{
    [Dependency] private readonly BarricadeSystem _barricade = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedOnCollideSystem _onCollide = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";

    private EntityQuery<BarricadeComponent> _barricadeQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;

    public override void Initialize()
    {
        _barricadeQuery = GetEntityQuery<BarricadeComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<XenoSprayAcidComponent, XenoSprayAcidActionEvent>(OnSprayAcidAction);
    }

    private void OnSprayAcidAction(Entity<XenoSprayAcidComponent> xeno, ref XenoSprayAcidActionEvent args)
    {
        args.Handled = true;

        if (_net.IsClient)
            return;

        var start = _mapSystem.AlignToGrid(xeno.Owner.ToCoordinates());
        var end = _mapSystem.AlignToGrid(args.Target);
        var distanceX = end.X - start.X;
        var distanceY = end.Y - start.Y;
        if (!start.TryDistance(EntityManager, _transform, end, out var distance))
            return;

        distance = MathF.Floor(distance);
        if (distance == 0)
            return;

        var x = start.X;
        var y = start.Y;
        var xOffset = distanceX / distance;
        var yOffset = distanceY / distance;
        var tiles = new List<(MapCoordinates Coordinates, TimeSpan At)>();
        var time = _timing.CurTime;
        var gridId = _transform.GetGrid(start.EntityId);
        var grid = gridId == null ? null : _mapGridQuery.CompOrNull(gridId.Value);

        EntityCoordinates? lastCoords = null;
        var delay = 0;
        for (var i = 0; i < distance; i++)
        {
            x += xOffset;
            y += yOffset;

            var entityCoords = new EntityCoordinates(start.EntityId, x, y).SnapToGrid(EntityManager, _mapManager);

            if (entityCoords == lastCoords)
                continue;

            if (lastCoords != null &&
                gridId != null &&
                grid != null)
            {
                var indices = _mapSystem.TileIndicesFor(gridId.Value, grid, entityCoords);
                var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(gridId.Value, grid, indices);
                var direction = (lastCoords.Value.Position - entityCoords.Position).ToWorldAngle().GetCardinalDir();
                var stop = false;
                while (anchored.MoveNext(out var uid))
                {
                    if (_barricadeQuery.HasComp(uid))
                    {
                        var barricadeDir = _transform.GetWorldRotation(uid.Value).GetCardinalDir();
                        if (barricadeDir == direction || barricadeDir == direction.GetOpposite())
                        {
                            stop = true;
                            break;
                        }
                    }
                    else if (_tag.HasTag(uid.Value, StructureTag))
                    {
                        stop = true;
                        break;
                    }
                }

                if (stop)
                    break;
            }

            var next = entityCoords.Offset(new Vector2(xOffset, yOffset));
            var nextDirection = (next.Position - entityCoords.Position).ToWorldAngle().GetCardinalDir();
            if (_barricade.HasBarricadeFacing(next, nextDirection.GetOpposite()))
                break;

            lastCoords = entityCoords;
            var mapCoords = _transform.ToMapCoordinates(entityCoords);
            tiles.Add((mapCoords, time + xeno.Comp.Delay * delay));
            delay++;
        }

        var active = EnsureComp<ActiveAcidSprayingComponent>(xeno);
        active.Acid = xeno.Comp.Acid;
        active.Spawn = tiles;
        Dirty(xeno, active);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var spraying = EntityQueryEnumerator<ActiveAcidSprayingComponent>();
        while (spraying.MoveNext(out var uid, out var active))
        {
            active.Chain ??= _onCollide.SpawnChain();
            for (var i = active.Spawn.Count - 1; i >= 0; i--)
            {
                var acid = active.Spawn[i];
                if (time < acid.At)
                    continue;

                var spawned = Spawn(active.Acid, acid.Coordinates);
                _onCollide.SetChain(spawned, active.Chain.Value);

                active.Spawn.RemoveAt(i);
            }

            if (active.Spawn.Count == 0)
                RemCompDeferred<ActiveAcidSprayingComponent>(uid);
        }
    }
}
