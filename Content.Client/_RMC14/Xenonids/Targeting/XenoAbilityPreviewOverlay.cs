using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Actions;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Smoke;
using Content.Shared._RMC14.Xenonids.Bombard;
using Content.Shared._RMC14.Xenonids.Burrow;
using Content.Shared._RMC14.Xenonids.Fruit.Components;
using Content.Shared._RMC14.Xenonids.ResinSurge;
using Content.Shared._RMC14.Xenonids.Spray;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared._RMC14.Xenonids.Abduct;
using Content.Shared._RMC14.Xenonids.Pierce;
using Content.Shared.Actions.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Targeting;

public sealed class XenoAbilityPreviewOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV | OverlaySpace.WorldSpace;

    private static readonly Color SprayOutlineColor = new Color(0.44f, 0.76f, 0.2f);
    private static readonly Color AbductOutlineColor = new Color(1f, 0.67f, 0.28f);
    private static readonly Color PierceOutlineColor = new Color(1f, 0.15f, 0.1f);
    private static readonly Color BombardFallbackColor = new Color(0.98f, 0.74f, 0.25f);
    private static readonly Color BurrowOutlineColor = new Color(0.95f, 0.85f, 0.2f);
    private static readonly Color ResinSurgeOutlineColor = new Color(0.34f, 0.87f, 0.57f);
    private static readonly Color InvalidOutlineColor = new Color(0.95f, 0.24f, 0.24f);
    private static readonly Color BlockerOutlineColor = new Color(0.65f, 0.65f, 0.65f);
    private const float OutlineAlpha = 0.8f;
    private const float OutlineThickness = 0.1f;
    private const int BombardDefaultRadius = 3;

    private readonly IInputManager _input;
    private readonly IEyeManager _eye;
    private readonly IPlayerManager _player;
    private readonly IUserInterfaceManager _ui;
    private readonly IConfigurationManager _config;
    private readonly IMapManager _mapManager;
    private readonly IPrototypeManager _prototypes;
    private readonly IComponentFactory _componentFactory;
    private readonly IStateManager _stateManager;
    private readonly SharedMapSystem _mapSystem;
    private readonly SharedPhysicsSystem _physics;
    private readonly SharedTransformSystem _transform;
    private readonly LineSystem _line;
    private readonly EntityQuery<ActionsComponent> _actionsQ;
    private readonly EntityQuery<TargetActionComponent> _targetActionQ;
    private readonly EntityQuery<WorldTargetActionComponent> _worldTargetQ;
    private readonly EntityQuery<XenoSprayAcidComponent> _sprayQ;
    private readonly EntityQuery<XenoBombardComponent> _bombardQ;
    private readonly EntityQuery<XenoBurrowComponent> _burrowQ;
    private readonly EntityQuery<XenoResinSurgeComponent> _resinSurgeQ;
    private readonly EntityQuery<ResinSurgeReinforcableComponent> _reinforcableQ;
    private readonly EntityQuery<XenoFruitComponent> _fruitQ;
    private readonly EntityQuery<XenoWeedsComponent> _weedsQ;
    private readonly EntityQuery<XenoAbductComponent> _abductQ;
    private readonly EntityQuery<XenoPierceComponent> _pierceQ;
    private readonly EntityQuery<TransformComponent> _xformQ;

    public XenoAbilityPreviewOverlay(IEntityManager ents)
    {
        _input = IoCManager.Resolve<IInputManager>();
        _eye = IoCManager.Resolve<IEyeManager>();
        _player = IoCManager.Resolve<IPlayerManager>();
        _ui = IoCManager.Resolve<IUserInterfaceManager>();
        _config = IoCManager.Resolve<IConfigurationManager>();
        _mapManager = IoCManager.Resolve<IMapManager>();
        _prototypes = IoCManager.Resolve<IPrototypeManager>();
        _componentFactory = IoCManager.Resolve<IComponentFactory>();
        _stateManager = IoCManager.Resolve<IStateManager>();
        _mapSystem = ents.System<SharedMapSystem>();
        _physics = ents.System<SharedPhysicsSystem>();
        _transform = ents.System<SharedTransformSystem>();
        _line = ents.System<LineSystem>();
        _actionsQ = ents.GetEntityQuery<ActionsComponent>();
        _targetActionQ = ents.GetEntityQuery<TargetActionComponent>();
        _worldTargetQ = ents.GetEntityQuery<WorldTargetActionComponent>();
        _sprayQ = ents.GetEntityQuery<XenoSprayAcidComponent>();
        _bombardQ = ents.GetEntityQuery<XenoBombardComponent>();
        _burrowQ = ents.GetEntityQuery<XenoBurrowComponent>();
        _resinSurgeQ = ents.GetEntityQuery<XenoResinSurgeComponent>();
        _reinforcableQ = ents.GetEntityQuery<ResinSurgeReinforcableComponent>();
        _fruitQ = ents.GetEntityQuery<XenoFruitComponent>();
        _weedsQ = ents.GetEntityQuery<XenoWeedsComponent>();
        _abductQ = ents.GetEntityQuery<XenoAbductComponent>();
        _pierceQ = ents.GetEntityQuery<XenoPierceComponent>();
        _xformQ = ents.GetEntityQuery<TransformComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_config.GetCVar(RMCCVars.RMCXenoAbilityPreviews))
            return;

        var player = _player.LocalEntity;
        if (player == null)
            return;

        if (!_xformQ.TryComp(player.Value, out var xform))
            return;

        var actionController = _ui.GetUIController<ActionUIController>();
        var originMap = _transform.GetMapCoordinates(player.Value, xform: xform);
        float? burrowRange = null;
        if (_burrowQ.TryComp(player.Value, out var burrow) &&
            IsBurrowed(burrow) &&
            args.Space == OverlaySpace.WorldSpace)
        {
            burrowRange = GetBurrowRange(player.Value, burrow, actionController.SelectingTargetFor);
            DrawBurrowRange(args, originMap, burrowRange.Value);
        }

        if (actionController.SelectingTargetFor is not { } action)
            return;

        var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);
        if (mousePos.MapId == MapId.Nullspace)
            return;

        if (originMap.MapId != mousePos.MapId)
            return;

        if (!_worldTargetQ.TryComp(action, out var worldTarget) || worldTarget.Event == null)
            return;

        if (args.Space == OverlaySpace.WorldSpace)
        {
            if (worldTarget.Event is not XenoBurrowActionEvent ||
                !_burrowQ.TryComp(player.Value, out burrow) ||
                !IsBurrowed(burrow))
            {
                return;
            }

            burrowRange ??= GetBurrowRange(player.Value, burrow, action);
            DrawBurrowTarget(args, originMap, mousePos, burrowRange.Value);
            return;
        }

        if (args.Space != OverlaySpace.WorldSpaceBelowFOV)
            return;

        switch (worldTarget.Event)
        {
            case XenoSprayAcidActionEvent:
                if (!_sprayQ.TryComp(player.Value, out var spray))
                    return;

                DrawSpray(args, player.Value, xform, originMap, mousePos, spray);
                break;
            case XenoBombardActionEvent:
                if (!_bombardQ.TryComp(player.Value, out var bombard))
                    return;

                DrawBombard(args, player.Value, xform, originMap, mousePos, bombard);
                break;
            case XenoResinSurgeActionEvent:
                if (!_resinSurgeQ.TryComp(player.Value, out var resinSurge))
                    return;

                DrawResinSurge(args, originMap, mousePos, resinSurge);
                break;

            case XenoAbductActionEvent:
                if (!_abductQ.TryComp(player.Value, out var abduct))
                    return;
                DrawAbduct(args, player.Value, xform, originMap, mousePos, abduct);
                break;

            case XenoPierceActionEvent:
                if (!_pierceQ.TryComp(player.Value, out var pierce))
                    return;
                DrawPierce(args, player.Value, xform, originMap, mousePos, pierce);
                break;
        }
    }

    private void DrawResinSurge(
        in OverlayDrawArgs args,
        MapCoordinates originMap,
        MapCoordinates mousePos,
        XenoResinSurgeComponent resinSurge)
    {
        var range = resinSurge.Range;
        if (range <= 0)
            return;

        if (!TryGetTileIndices(mousePos, out var targetTile))
            return;

        var targetCenter = _mapSystem.GridTileToWorld(targetTile.GridUid, targetTile.Grid, targetTile.Indices);
        var valid = (targetCenter.Position - originMap.Position).LengthSquared() <= range * range;
        var color = (valid ? ResinSurgeOutlineColor : InvalidOutlineColor).WithAlpha(OutlineAlpha);

        if (TryGetResinSurgeDirectTarget(mousePos, out var directTargetTile))
        {
            DrawTileMarker(args.WorldHandle, directTargetTile, color);
            return;
        }

        DrawResinSurgeSquare(args.WorldHandle, originMap, targetTile, resinSurge.StickyResinRadius);
    }

    private void DrawSpray(
        in OverlayDrawArgs args,
        EntityUid player,
        TransformComponent xform,
        MapCoordinates originMap,
        MapCoordinates mousePos,
        XenoSprayAcidComponent spray)
    {
        var direction = mousePos.Position - originMap.Position;
        if (direction.Length() > spray.Range)
            mousePos = originMap.Offset(direction.Normalized() * spray.Range);

        var color = SprayOutlineColor.WithAlpha(OutlineAlpha);
        DrawLinePreview(args, player, xform.Coordinates, mousePos, spray.Range, color);
    }

    private void DrawAbduct(
        in OverlayDrawArgs args,
        EntityUid player,
        TransformComponent xform,
        MapCoordinates originMap,
        MapCoordinates mousePos,
        XenoAbductComponent abduct)
    {
        var direction = mousePos.Position - originMap.Position;
        if (direction.Length() > abduct.Range)
            mousePos = originMap.Offset(direction.Normalized() * abduct.Range);

        var color = AbductOutlineColor.WithAlpha(OutlineAlpha);
        DrawLinePreview(args, player, xform.Coordinates, mousePos, abduct.Range, color);
    }

    private void DrawPierce(
        in OverlayDrawArgs args,
        EntityUid player,
        TransformComponent xform,
        MapCoordinates originMap,
        MapCoordinates mousePos,
        XenoPierceComponent pierce)
    {
        var direction = mousePos.Position - originMap.Position;
        if (direction.Length() > pierce.Range)
            mousePos = originMap.Offset(direction.Normalized() * (int)pierce.Range);

        var color = PierceOutlineColor.WithAlpha(OutlineAlpha);
        DrawLinePreview(args, player, xform.Coordinates, mousePos, (int)pierce.Range, color);
    }

    private void DrawBombard(
        in OverlayDrawArgs args,
        EntityUid player,
        TransformComponent xform,
        MapCoordinates originMap,
        MapCoordinates mousePos,
        XenoBombardComponent bombard)
    {
        var direction = mousePos.Position - originMap.Position;
        if (direction.Length() > bombard.Range)
            mousePos = originMap.Offset(direction.Normalized() * bombard.Range);

        var radius = GetBombardRadius(bombard.Projectile);
        var baseColor = GetBombardColor(bombard.Projectile);
        var color = baseColor.WithAlpha(OutlineAlpha);

        var impact = mousePos;
        var collisionMask = GetProjectileCollisionMask(bombard.Projectile);
        if (TryGetProjectileImpact(originMap, mousePos, collisionMask, player, out var hitEntity, out var hitCoordinates))
        {
            impact = hitCoordinates;
        }

        impact = AdjustProjectileImpact(bombard.Projectile, originMap, impact);

        var toCoordinates = _transform.ToCoordinates(player, impact);
        if (hitEntity != null && TryGetEntityTile(hitEntity.Value, out var blockerInfo))
        {
            DrawTileMarker(args.WorldHandle, blockerInfo, BlockerOutlineColor.WithAlpha(OutlineAlpha));
        }

        if (!_mapManager.TryFindGridAt(impact, out var gridUid, out var grid))
        {
            args.WorldHandle.DrawCircle(impact.Position, radius, color, false);
            return;
        }

        var center = _mapSystem.CoordinatesToTile(gridUid, grid, impact);
        var aoeTiles = new HashSet<Vector2i>();
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                if (new Vector2(x, y).Length() > radius)
                    continue;

                aoeTiles.Add(center + new Vector2i(x, y));
            }
        }

        DrawTileBorder(args.WorldHandle, gridUid, grid, aoeTiles, color);
    }

    private void DrawBurrowRange(
        in OverlayDrawArgs args,
        MapCoordinates originMap,
        float range)
    {
        if (range <= 0f)
            return;

        var color = BurrowOutlineColor.WithAlpha(OutlineAlpha);
        DrawTileRange(args, originMap, range, color);
    }

    private void DrawTileRange(
        in OverlayDrawArgs args,
        MapCoordinates originMap,
        float range,
        Color color)
    {
        if (!_mapManager.TryFindGridAt(originMap, out var gridUid, out var grid))
            return;

        var center = _mapSystem.CoordinatesToTile(gridUid, grid, originMap);
        var tileSize = grid.TileSize;
        var maxTiles = (int) MathF.Ceiling(range / tileSize);
        var tiles = new HashSet<Vector2i>();
        for (var x = -maxTiles; x <= maxTiles; x++)
        {
            for (var y = -maxTiles; y <= maxTiles; y++)
            {
                var distance = new Vector2(x * tileSize, y * tileSize).Length();
                if (distance > range)
                    continue;

                tiles.Add(center + new Vector2i(x, y));
            }
        }

        DrawTileBorder(args.WorldHandle, gridUid, grid, tiles, color);
    }

    private void DrawBurrowTarget(
        in OverlayDrawArgs args,
        MapCoordinates originMap,
        MapCoordinates mousePos,
        float range)
    {
        if (range <= 0f)
            return;

        var direction = mousePos.Position - originMap.Position;
        if (direction.Length() > range)
            mousePos = originMap.Offset(direction.Normalized() * range);

        var color = BurrowOutlineColor.WithAlpha(OutlineAlpha);
        DrawLandingTile(args, mousePos, color);
    }

    private static bool IsBurrowed(XenoBurrowComponent burrow)
    {
        return burrow.Active || burrow.Tunneling || burrow.ForcedUnburrowAt != null;
    }

    private float GetBurrowRange(EntityUid player, XenoBurrowComponent burrow, EntityUid? selectedAction)
    {
        var maxRange = burrow.MaxTunnelingDistance;
        float? actionRange = null;

        if (selectedAction != null && TryGetBurrowActionRange(selectedAction.Value, out var selectedRange))
        {
            actionRange = selectedRange;
        }
        else if (_actionsQ.TryComp(player, out var actions))
        {
            foreach (var action in actions.Actions)
            {
                if (!TryGetBurrowActionRange(action, out var range))
                    continue;

                actionRange = range;
                break;
            }
        }

        if (actionRange != null)
            maxRange = Math.Min(maxRange, actionRange.Value);

        return maxRange;
    }

    private bool TryGetBurrowActionRange(EntityUid action, out float range)
    {
        range = default;
        if (!_worldTargetQ.TryComp(action, out var worldTarget) || worldTarget.Event is not XenoBurrowActionEvent)
            return false;

        if (!_targetActionQ.TryComp(action, out var targetAction))
            return false;

        range = targetAction.Range;
        return true;
    }

    private void DrawLinePreview(
        in OverlayDrawArgs args,
        EntityUid player,
        EntityCoordinates fromCoordinates,
        MapCoordinates target,
        float range,
        Color color)
    {
        var toCoordinates = _transform.ToCoordinates(player, target);
        var tiles = _line.DrawLine(fromCoordinates, toCoordinates, TimeSpan.Zero, range, out var blocker, hitBlocker: true);
        if (tiles.Count == 0)
            return;

        DrawTileBorderFromLineTiles(args, tiles, color);

        if (blocker != null && TryGetEntityTile(blocker.Value, out var blockerInfo))
        {
            DrawTileMarker(args.WorldHandle, blockerInfo, BlockerOutlineColor.WithAlpha(OutlineAlpha));
        }
    }

    private void DrawTileBorderFromLineTiles(in OverlayDrawArgs args, List<LineTile> tiles, Color color)
    {
        var tilesByGrid = new Dictionary<EntityUid, TileSet>();
        foreach (var tile in tiles)
        {
            if (tile.Coordinates.MapId != args.MapId)
                continue;

            if (!_mapManager.TryFindGridAt(tile.Coordinates, out var gridUid, out var grid))
                continue;

            var indices = _mapSystem.CoordinatesToTile(gridUid, grid, tile.Coordinates);
            if (!tilesByGrid.TryGetValue(gridUid, out var set))
            {
                set = new TileSet(grid);
                tilesByGrid.Add(gridUid, set);
            }

            set.Tiles.Add(indices);
        }

        foreach (var (gridUid, set) in tilesByGrid)
        {
            DrawTileBorder(args.WorldHandle, gridUid, set.Grid, set.Tiles, color);
        }
    }

    private void DrawLandingTile(in OverlayDrawArgs args, MapCoordinates target, Color color)
    {
        if (!_mapManager.TryFindGridAt(target, out var gridUid, out var grid))
            return;

        var indices = _mapSystem.CoordinatesToTile(gridUid, grid, target);
        var tiles = new HashSet<Vector2i> { indices };
        DrawTileBorder(args.WorldHandle, gridUid, grid, tiles, color);
    }

    private bool TryGetResinSurgeDirectTarget(MapCoordinates mousePos, out TileInfo target)
    {
        target = default;
        if (_stateManager.CurrentState is not GameplayStateBase screen)
            return false;

        var entity = screen.GetClickedEntity(mousePos);
        if (entity == null ||
            (!_reinforcableQ.HasComp(entity.Value) &&
             !_fruitQ.HasComp(entity.Value) &&
             !_weedsQ.HasComp(entity.Value)))
        {
            return false;
        }

        return TryGetEntityTile(entity.Value, out target);
    }

    private void DrawTileSquare(DrawingHandleWorld handle, TileInfo center, int radius, Color color)
    {
        var tiles = new HashSet<Vector2i>();
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                tiles.Add(center.Indices + new Vector2i(x, y));
            }
        }

        DrawTileBorder(handle, center.GridUid, center.Grid, tiles, color);
    }

    private void DrawResinSurgeSquare(DrawingHandleWorld handle, MapCoordinates originMap, TileInfo center, int radius)
    {
        var validTiles = new HashSet<Vector2i>();
        var invalidTiles = new HashSet<Vector2i>();
        var rangeSquared = MathF.Pow(_resinSurgeQ.GetComponent(_player.LocalEntity!.Value).Range, 2);

        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                var tile = center.Indices + new Vector2i(x, y);
                var tileCenter = _mapSystem.GridTileToWorld(center.GridUid, center.Grid, tile);
                if ((tileCenter.Position - originMap.Position).LengthSquared() <= rangeSquared)
                    validTiles.Add(tile);
                else
                    invalidTiles.Add(tile);
            }
        }

        DrawTileBorder(handle, center.GridUid, center.Grid, validTiles, ResinSurgeOutlineColor.WithAlpha(OutlineAlpha));
        DrawTileBorder(handle, center.GridUid, center.Grid, invalidTiles, InvalidOutlineColor.WithAlpha(OutlineAlpha));
    }

    private bool TryGetTileIndices(MapCoordinates coordinates, out TileInfo info)
    {
        info = default;
        if (!_mapManager.TryFindGridAt(coordinates, out var gridUid, out var grid))
            return false;

        var indices = _mapSystem.CoordinatesToTile(gridUid, grid, coordinates);
        info = new TileInfo(gridUid, grid, indices);
        return true;
    }

    private bool TryGetEntityTile(EntityUid entity, out TileInfo info)
    {
        var coordinates = _transform.GetMapCoordinates(entity);
        return TryGetTileIndices(coordinates, out info);
    }

    private void DrawTileMarker(DrawingHandleWorld handle, TileInfo info, Color color)
    {
        var tiles = new HashSet<Vector2i> { info.Indices };
        DrawTileBorder(handle, info.GridUid, info.Grid, tiles, color);
    }

    private void DrawTileBorder(DrawingHandleWorld handle, EntityUid gridUid, MapGridComponent grid, HashSet<Vector2i> tiles, Color color)
    {
        if (tiles.Count == 0)
            return;

        var tileSize = grid.TileSize;
        var tileSizeVec = new Vector2(tileSize, tileSize);

        foreach (var indices in tiles)
        {
            var baseLocal = new Vector2(indices.X * tileSize, indices.Y * tileSize);
            var p00 = _transform.ToMapCoordinates(new EntityCoordinates(gridUid, baseLocal)).Position;
            var p10 = _transform.ToMapCoordinates(new EntityCoordinates(gridUid, baseLocal + new Vector2(tileSize, 0f))).Position;
            var p11 = _transform.ToMapCoordinates(new EntityCoordinates(gridUid, baseLocal + tileSizeVec)).Position;
            var p01 = _transform.ToMapCoordinates(new EntityCoordinates(gridUid, baseLocal + new Vector2(0f, tileSize))).Position;

            if (!tiles.Contains(new Vector2i(indices.X, indices.Y + 1)))
                DrawEdge(handle, p01, p11, color);
            if (!tiles.Contains(new Vector2i(indices.X, indices.Y - 1)))
                DrawEdge(handle, p00, p10, color);
            if (!tiles.Contains(new Vector2i(indices.X + 1, indices.Y)))
                DrawEdge(handle, p10, p11, color);
            if (!tiles.Contains(new Vector2i(indices.X - 1, indices.Y)))
                DrawEdge(handle, p00, p01, color);
        }
    }

    private int GetBombardRadius(EntProtoId projectile)
    {
        if (!_prototypes.TryIndex<EntityPrototype>(projectile, out var projectileProto))
            return BombardDefaultRadius;

        if (!projectileProto.TryGetComponent<SpawnOnTerminateComponent>(out var spawn, _componentFactory))
            return BombardDefaultRadius;

        if (!_prototypes.TryIndex<EntityPrototype>(spawn.Spawn, out var smokeProto))
            return BombardDefaultRadius;

        if (smokeProto.TryGetComponent<EvenSmokeComponent>(out var evenSmoke, _componentFactory))
            return evenSmoke.Range;

        return BombardDefaultRadius;
    }

    private Color GetBombardColor(EntProtoId projectile)
    {
        if (_prototypes.TryIndex<EntityPrototype>(projectile, out var projectileProto) &&
            projectileProto.TryGetComponent<SpawnOnTerminateComponent>(out var spawn, _componentFactory) &&
            _prototypes.TryIndex<EntityPrototype>(spawn.Spawn, out var smokeProto) &&
            smokeProto.TryGetComponent<SpriteComponent>(out var sprite, _componentFactory))
        {
            return sprite.Color;
        }

        return BombardFallbackColor;
    }

    private int GetProjectileCollisionMask(EntProtoId projectile)
    {
        if (_prototypes.TryIndex<EntityPrototype>(projectile, out var projectileProto) &&
            projectileProto.TryGetComponent<FixturesComponent>(out var fixtures, _componentFactory))
        {
            var mask = 0;
            foreach (var fixture in fixtures.Fixtures.Values)
            {
                mask |= fixture.CollisionMask;
            }

            if (mask != 0)
                return mask;
        }

        return (int) (CollisionGroup.Impassable | CollisionGroup.BulletImpassable | CollisionGroup.XenoProjectileImpassable);
    }

    private MapCoordinates AdjustProjectileImpact(EntProtoId projectile, MapCoordinates origin, MapCoordinates impact)
    {
        if (_prototypes.TryIndex<EntityPrototype>(projectile, out var projectileProto) &&
            projectileProto.TryGetComponent<SpawnOnTerminateComponent>(out var spawn, _componentFactory) &&
            spawn.ProjectileAdjust)
        {
            var delta = impact.Position - origin.Position;
            if (delta.LengthSquared() > 0f)
                return impact.Offset(delta.Normalized() * -0.5f);
        }

        return impact;
    }

    private bool TryGetProjectileImpact(
        MapCoordinates origin,
        MapCoordinates target,
        int collisionMask,
        EntityUid? ignored,
        out EntityUid? hitEntity,
        out MapCoordinates hitCoordinates)
    {
        hitEntity = null;
        hitCoordinates = target;

        var direction = target.Position - origin.Position;
        var distance = direction.Length();
        if (distance <= 0f)
            return false;

        var ray = new CollisionRay(origin.Position, direction / distance, collisionMask);
        foreach (var result in _physics.IntersectRay(origin.MapId, ray, distance, ignored, returnOnFirstHit: true))
        {
            hitEntity = result.HitEntity;
            hitCoordinates = new MapCoordinates(result.HitPos, origin.MapId);
            return true;
        }

        return false;
    }

    private static void DrawEdge(DrawingHandleWorld handle, Vector2 from, Vector2 to, Color color)
    {
        DrawSegment(handle, from, to, color, OutlineThickness);
    }

    private static void DrawSegment(DrawingHandleWorld handle, Vector2 from, Vector2 to, Color color, float thickness)
    {
        var delta = to - from;
        if (delta.LengthSquared() <= 0f || thickness <= 0f)
        {
            handle.DrawLine(from, to, color);
            return;
        }

        var half = thickness * 0.5f;
        if (Math.Abs(delta.X) < 0.001f)
        {
            var x = from.X;
            var minY = Math.Min(from.Y, to.Y);
            var maxY = Math.Max(from.Y, to.Y);
            var box = new Box2(new Vector2(x - half, minY), new Vector2(x + half, maxY));
            handle.DrawRect(box, color);
            return;
        }

        if (Math.Abs(delta.Y) < 0.001f)
        {
            var y = from.Y;
            var minX = Math.Min(from.X, to.X);
            var maxX = Math.Max(from.X, to.X);
            var box = new Box2(new Vector2(minX, y - half), new Vector2(maxX, y + half));
            handle.DrawRect(box, color);
            return;
        }

        var length = delta.Length();
        var mid = (from + to) * 0.5f;
        var angle = delta.ToWorldAngle();
        var rect = new Box2(-length / 2f, -half, length / 2f, half);
        var rotated = new Box2Rotated(rect.Translated(mid), angle, mid);
        handle.DrawRect(rotated, color);
    }

    private readonly record struct TileInfo(EntityUid GridUid, MapGridComponent Grid, Vector2i Indices);

    private sealed class TileSet
    {
        public readonly MapGridComponent Grid;
        public readonly HashSet<Vector2i> Tiles = new();

        public TileSet(MapGridComponent grid)
        {
            Grid = grid;
        }
    }
}
