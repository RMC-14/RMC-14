using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.OnCollide;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Spray;

public sealed class XenoSprayAcidSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedOnCollideSystem _onCollide = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";
    private static readonly ProtoId<ReagentPrototype> AcidRemovedBy = "Water";

    private EntityQuery<BarricadeComponent> _barricadeQuery;
    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;
    private EntityQuery<XenoSprayAcidComponent> _xenoSprayAcidQuery;

    public override void Initialize()
    {
        _barricadeQuery = GetEntityQuery<BarricadeComponent>();
        _doorQuery = GetEntityQuery<DoorComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _xenoSprayAcidQuery = GetEntityQuery<XenoSprayAcidComponent>();

        SubscribeLocalEvent<XenoSprayAcidComponent, XenoSprayAcidActionEvent>(OnSprayAcidAction);

        SubscribeLocalEvent<SprayAcidedComponent, MapInitEvent>(OnSprayAcidedMapInit);
        SubscribeLocalEvent<SprayAcidedComponent, ComponentRemove>(OnSprayAcidedRemove);
        SubscribeLocalEvent<SprayAcidedComponent, VaporHitEvent>(OnSprayAcidedVaporHit);
    }

    private void OnSprayAcidAction(Entity<XenoSprayAcidComponent> xeno, ref XenoSprayAcidActionEvent args)
    {
        args.Handled = true;
        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);

        if (_net.IsClient)
            return;

        var start = _mapSystem.AlignToGrid(_transform.GetMoverCoordinates(xeno.Owner.ToCoordinates()));
        var end = _mapSystem.AlignToGrid(_transform.GetMoverCoordinates(args.Target));
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
        var tiles = new List<(MapCoordinates Coordinates, TimeSpan At, Direction Direction)>();
        var time = _timing.CurTime;
        var gridId = _transform.GetGrid(start.EntityId);
        var gridComp = gridId == null ? null : _mapGridQuery.CompOrNull(gridId.Value);
        Entity<MapGridComponent>? grid = gridComp == null ? null : new Entity<MapGridComponent>(gridId!.Value, gridComp);
        var lastCoords = start;
        var delay = 0;

        for (var i = 0; i < distance; i++)
        {
            x += xOffset;
            y += yOffset;

            var entityCoords = new EntityCoordinates(start.EntityId, x, y).SnapToGrid(EntityManager, _mapManager);
            if (entityCoords == lastCoords)
                continue;

            var direction = (entityCoords.Position - lastCoords.Position).ToWorldAngle().GetCardinalDir();
            var blocked = IsTileBlocked(grid, entityCoords, direction);
            if (blocked)
                break;

            lastCoords = entityCoords;
            var mapCoords = _transform.ToMapCoordinates(entityCoords);
            tiles.Add((mapCoords, time + xeno.Comp.Delay * delay, direction));
            delay++;
        }

        var active = EnsureComp<ActiveAcidSprayingComponent>(xeno);
        active.Acid = xeno.Comp.Acid;
        active.Spawn = tiles;
        Dirty(xeno, active);
    }

    private void OnSprayAcidedMapInit(Entity<SprayAcidedComponent> ent, ref MapInitEvent args)
    {
        _appearance.SetData(ent, SprayAcidedVisuals.Acided, true);
    }

    private void OnSprayAcidedRemove(Entity<SprayAcidedComponent> ent, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(ent))
            _appearance.SetData(ent, SprayAcidedVisuals.Acided, false);
    }

    private void OnSprayAcidedVaporHit(Entity<SprayAcidedComponent> ent, ref VaporHitEvent args)
    {
        // this would use tile reactions if those had any way of telling what caused a reaction, imagine that
        var solEnt = args.Solution;
        foreach (var (_, solution) in _solutionContainer.EnumerateSolutions((solEnt, solEnt)))
        {
            if (!solution.Comp.Solution.ContainsReagent(AcidRemovedBy, null))
                continue;

            RemCompDeferred<SprayAcidedComponent>(ent);
            break;
        }
    }

    private bool IsTileBlocked(Entity<MapGridComponent>? grid, EntityCoordinates coords, Direction direction)
    {
        if (grid == null)
            return false;

        var indices = _mapSystem.TileIndicesFor(grid.Value, grid, coords);
        var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(grid.Value, grid, indices);
        while (anchored.MoveNext(out var uid))
        {
            if (_barricadeQuery.HasComp(uid))
            {
                if (_doorQuery.TryComp(uid, out var door) && door.State != DoorState.Closed)
                    continue;

                var barricadeDir = _transform.GetWorldRotation(uid.Value).GetCardinalDir();
                if (barricadeDir == direction || barricadeDir == direction.GetOpposite())
                    return true;
            }
            else if (_tag.HasTag(uid.Value, StructureTag))
            {
                return true;
            }
        }

        return false;
    }

    private void TryAcid(Entity<XenoSprayAcidComponent> acid, RMCAnchoredEntitiesEnumerator anchored)
    {
        var time = _timing.CurTime;
        while (anchored.MoveNext(out var uid))
        {
            if (!_barricadeQuery.HasComp(uid))
                continue;

            var comp = EnsureComp<SprayAcidedComponent>(uid);
            comp.DamagePerSecond = acid.Comp.BarricadeDamage;
            comp.ExpireAt = time + acid.Comp.BarricadeDuration;
            Dirty(uid, comp);
        }
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
                if (_xenoSprayAcidQuery.TryComp(uid, out var xenoSprayAcid))
                {
                    var spray = new Entity<XenoSprayAcidComponent>(uid, xenoSprayAcid);

                    // Same tile
                    TryAcid(spray, _rmcMap.GetAnchoredEntitiesEnumerator(spawned));

                    // Sides
                    var direction = acid.Direction;
                    var (first, second) = direction.GetPerpendiculars();
                    TryAcid(spray, _rmcMap.GetAnchoredEntitiesEnumerator(spawned, first, first.GetOpposite().AsFlag()));
                    TryAcid(spray, _rmcMap.GetAnchoredEntitiesEnumerator(spawned, second, second.GetOpposite().AsFlag()));

                    // Ahead
                    var aheadDirection = direction.AsFlag() | direction.GetOpposite().AsFlag();
                    TryAcid(spray, _rmcMap.GetAnchoredEntitiesEnumerator(spawned, direction, aheadDirection));
                }

                _onCollide.SetChain(spawned, active.Chain.Value);

                active.Spawn.RemoveAt(i);
            }

            if (active.Spawn.Count == 0)
                RemCompDeferred<ActiveAcidSprayingComponent>(uid);
        }

        var acidedQuery = EntityQueryEnumerator<SprayAcidedComponent>();
        while (acidedQuery.MoveNext(out var uid, out var acided))
        {
            if (time >= acided.ExpireAt)
            {
                RemCompDeferred<SprayAcidedComponent>(uid);
                continue;
            }

            if (time < acided.NextDamageAt)
                continue;

            acided.NextDamageAt = time + acided.DamageEvery;
            _damageable.TryChangeDamage(uid, acided.DamagePerSecond);
        }
    }
}
