using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Maps;
using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using static Content.Shared.Physics.CollisionGroup;

namespace Content.Shared._CM14.Entrenching;

public sealed class EntrenchingToolSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly ITileDefinitionManager _tiles = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EntrenchingToolComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<EntrenchingToolComponent, EntrenchingToolDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<EntrenchingToolComponent, ItemToggledEvent>(OnItemToggled);

        SubscribeLocalEvent<EmptySandbagComponent, InteractUsingEvent>(OnEmptyInteractUsing);

        SubscribeLocalEvent<FullSandbagComponent, ActivateInWorldEvent>(OnFullActivateInWorld);
        SubscribeLocalEvent<FullSandbagComponent, AfterInteractEvent>(OnFullAfterInteract);
        SubscribeLocalEvent<FullSandbagComponent, SandbagBuildDoAfterEvent>(OnFullBuildDoAfter);
    }

    private void OnAfterInteract(Entity<EntrenchingToolComponent> tool, ref AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (!CanDig(tool, args.ClickLocation, out var grid, out var tile))
            return;

        var coordinates = _mapSystem.GridTileToLocal(grid, grid, tile.GridIndices);
        var ev = new EntrenchingToolDoAfterEvent(GetNetCoordinates(coordinates));
        var doAfter = new DoAfterArgs(EntityManager, args.User, tool.Comp.DigDelay, ev, tool, used: tool)
        {
            BreakOnUserMove = true
        };

        _doAfter.TryStartDoAfter(doAfter);
        args.Handled = true;
    }

    private void OnDoAfter(Entity<EntrenchingToolComponent> tool, ref EntrenchingToolDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var coordinates = GetCoordinates(args.Coordinates);
        if (!CanDig(tool, coordinates, out _, out _))
            return;

        if (!_interaction.InRangeUnobstructed(args.User, coordinates, popup: false))
            return;

        tool.Comp.TotalLayers = tool.Comp.LayersPerDig;
        Dirty(tool);
        args.Handled = true;
    }

    private void OnItemToggled(Entity<EntrenchingToolComponent> tool, ref ItemToggledEvent args)
    {
        tool.Comp.TotalLayers = 0;
        Dirty(tool);
    }

    private void OnEmptyInteractUsing(Entity<EmptySandbagComponent> empty, ref InteractUsingEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.Handled)
            return;

        if (!TryComp(args.Used, out EntrenchingToolComponent? tool) ||
            tool.TotalLayers <= 0)
        {
            return;
        }

        tool.TotalLayers--;
        Dirty(args.Used, tool);

        var coordinates = _transform.GetMoverCoordinates(empty);
        var filled = Spawn(empty.Comp.Filled, coordinates);

        if (TryComp(empty, out StackComponent? stack))
        {
            var stackCount = _stack.GetCount(empty, stack);
            _stack.SetCount(empty, stackCount - 1, stack);

            var filledStack = EnsureComp<StackComponent>(filled);
            _stack.SetCount(filled, 1, filledStack);
        }
        else
        {
            Del(empty);
        }

        args.Handled = true;
    }

    private void OnFullActivateInWorld(Entity<FullSandbagComponent> full, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !TryComp(args.User, out TransformComponent? transform))
        {
            return;
        }

        var coordinates = _transform.GetMoverCoordinates(args.User, transform);
        var direction = transform.LocalRotation.GetCardinalDir();
        if (Build(full, args.User, coordinates, direction))
            args.Handled = true;
    }

    private void OnFullAfterInteract(Entity<FullSandbagComponent> full, ref AfterInteractEvent args)
    {
        if (args.Handled || !TryComp(args.User, out TransformComponent? transform))
            return;

        var direction = transform.LocalRotation.GetCardinalDir();
        if (Build(full, args.User, args.ClickLocation, direction))
            args.Handled = true;
    }

    private void OnFullBuildDoAfter(Entity<FullSandbagComponent> full, ref SandbagBuildDoAfterEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.Cancelled || args.Handled)
            return;

        var coordinates = GetCoordinates(args.Coordinates);
        if (!_mapManager.TryFindGridAt(coordinates.ToMap(EntityManager, _transform), out var gridId, out var gridComp) ||
            !_interaction.InRangeUnobstructed(full, coordinates, popup: false) ||
            !coordinates.TryGetTileRef(out var turf, EntityManager) ||
            !CanBuild(full, (gridId, gridComp), args.User, turf.Value, args.Direction))
        {
            return;
        }

        if (full.Comp.StackRequired > 1)
        {
            var count = _stack.GetCount(full);
            if (count < full.Comp.StackRequired)
                return;

            if (TryComp(full, out StackComponent? fullStack))
                _stack.SetCount(full, count - full.Comp.StackRequired, fullStack);
            else
                QueueDel(full);
        }

        var built = SpawnAtPosition(full.Comp.Builds, coordinates);
        _transform.SetLocalRotation(built, args.Direction.ToAngle());

        args.Handled = true;
    }

    private bool Build(Entity<FullSandbagComponent> full, EntityUid user, EntityCoordinates coordinates, Direction direction)
    {
        if (!_mapManager.TryFindGridAt(coordinates.ToMap(EntityManager, _transform), out var gridId, out var gridComp) ||
            !coordinates.TryGetTileRef(out var tile) ||
            !CanBuild(full, (gridId, gridComp), user, tile.Value, direction))
        {
            return false;
        }

        var ev = new SandbagBuildDoAfterEvent(GetNetCoordinates(coordinates), direction);
        var doAfter = new DoAfterArgs(EntityManager, user, full.Comp.BuildDelay, ev, full, full)
        {
            BreakOnUserMove = true
        };

        _doAfter.TryStartDoAfter(doAfter);
        return true;
    }

    private bool CanDig(Entity<EntrenchingToolComponent> tool, EntityCoordinates coordinates, out Entity<MapGridComponent> grid, out TileRef tileRef)
    {
        grid = default;
        tileRef = default;

        if (TryComp(tool, out ItemToggleComponent? toggle) && !toggle.Activated)
            return false;

        if (!_mapManager.TryFindGridAt(coordinates.ToMap(EntityManager, _transform), out var gridId, out var gridComp))
            return false;

        tileRef = _mapSystem.GetTileRef(gridId, gridComp, coordinates);
        var tileDef = (ContentTileDefinition) _tiles[tileRef.Tile.TypeId];
        if (!tileDef.CanDig)
            return false;

        if (!TileSolidAndNotBlocked(tileRef))
            return false;

        grid = (gridId, gridComp);
        return true;
    }

    private bool TileSolidAndNotBlocked(TileRef tile)
    {
        return !tile.IsSpace() &&
               tile.GetContentTileDefinition().Sturdy &&
               !_turf.IsTileBlocked(tile, Impassable);
    }

    private bool CanBuild(
        Entity<FullSandbagComponent> full,
        Entity<MapGridComponent> grid,
        EntityUid user,
        TileRef tile,
        Direction direction)
    {
        if (!TryComp(full, out StackComponent? stack) ||
            stack.Count < 5)
        {
            return false;
        }

        var coordinates = new EntityCoordinates(tile.GridUid, tile.X, tile.Y).Offset(grid.Comp.TileSizeHalfVector);
        var mask = Impassable | InteractImpassable | TableLayer;
        var popup = _net.IsClient;
        if (!_interaction.InRangeUnobstructed(user, coordinates, collisionMask: mask, popup: popup))
            return false;

        if (!TileSolidAndNotBlocked(tile))
            return false;

        var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(grid, grid, tile.GridIndices);
        while (anchored.MoveNext(out var uid))
        {
            if (HasComp<BarricadeComponent>(uid) &&
                TryComp(uid, out TransformComponent? transform) &&
                transform.LocalRotation.GetCardinalDir() == direction)
            {
                return false;
            }
        }

        return true;
    }
}
