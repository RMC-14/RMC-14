using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Numerics;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Network; // Для INetManager
using Robust.Shared.Map.Components; // Для MapGridComponent
using Robust.Shared.Physics.Components; // Для PhysicsComponent
using Robust.Shared.GameObjects; // Для ActorComponent
using Content.Shared.Mobs.Components; // Для MobStateComponent
using Content.Shared.Buckle.Components; // Для StrapComponent
using Content.Shared._RMC14.Rules; // Для RMCPlanetComponent и RMCPlanetSystem
using Content.Shared._RMC14.Deploy; // Для ShowDeployAreaEvent и HideDeployAreaEvent
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.Deploy;

public sealed class RMCDeploySystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly Content.Shared.Maps.TurfSystem _turf = default!;
    [Dependency] private readonly Content.Shared.Tag.TagSystem _tags = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCDeployableComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCDeployableComponent, RMCDeployDoAfterEvent>(OnDoAfter);
    }

    private void OnUseInHand(EntityUid uid, RMCDeployableComponent comp, UseInHandEvent args)
    {
        TryStartDeploy(uid, args.User, comp);
    }

    private void TryStartDeploy(EntityUid uid, EntityUid user, RMCDeployableComponent comp)
    {
        var userXform = _xform.GetWorldPosition(user);
        var areaCenter = userXform;
        var transform = new Robust.Shared.Physics.Transform(areaCenter, 0);
        var area = comp.DeployArea.ComputeAABB(transform, 0);

        // Проверка занятости области (если не отключена)
        if (!comp.SkipAreaOccupiedCheck && IsAreaOccupied(area, uid, user, comp))
            return;

        var doAfter = new DoAfterArgs(_entMan, user, comp.DeployTime, new RMCDeployDoAfterEvent(), uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            NeedHand = true,
            ExtraCheck = comp.SkipAreaOccupiedCheck ? null : () => !IsAreaOccupied(area, uid, user, comp)
        };
        _doAfter.TryStartDoAfter(doAfter);

        // Отправляем событие для отображения зоны деплоя на клиенте
        if (_netManager.IsServer)
        {
            var center = new System.Numerics.Vector2(area.Center.X, area.Center.Y); // Преобразование типов
            var showEvent = new ShowDeployAreaEvent(
                center,
                area.Width,
                area.Height,
                Color.Blue // Цвет границы зоны
            );
            RaiseNetworkEvent(showEvent, user);
        }
    }

    private void OnDoAfter(EntityUid uid, RMCDeployableComponent comp, RMCDeployDoAfterEvent ev)
    {
        if (_netManager.IsClient)
            return;

        if (ev.Cancelled || ev.Handled)
        {
            RaiseNetworkEvent(new HideDeployAreaEvent(), ev.Args.User);
            return;
        }

        ev.Handled = true;

        // Спавним только если не отменено
        if (!ev.Cancelled)
        {
            var user = ev.Args.User;
            var userXform = _xform.GetWorldPosition(user);
            var areaTransform = new Robust.Shared.Physics.Transform(userXform, 0);
            var area = comp.DeployArea.ComputeAABB(areaTransform, 0);
            var areaCenter = area.Center;

            foreach (var setup in comp.DeploySetups)
            {
                var spawnPos = areaCenter + setup.Offset;
                var ent = EntityManager.SpawnEntity(setup.Prototype, new MapCoordinates(spawnPos, _xform.GetMapId(user)));
                _xform.SetWorldPosition(ent, spawnPos);
                _xform.SetWorldRotation(ent, Angle.FromDegrees(setup.Angle));
                if (setup.AnchorToTileCenter)
                {
                    _xform.AnchorEntity((ent, Transform(ent)));
                }
            }

            // Удаляем мешок после развертывания
            EntityManager.DeleteEntity(uid);
        }
    }

    private bool IsAreaOccupied(Box2 area, EntityUid ignore, EntityUid? user = null, RMCDeployableComponent? deployable = null)
    {
        var mapId = _xform.GetMapId(ignore);
        var found = false;
        var checkMobs = deployable?.CheckMobsInArea ?? true;
        var failIfNotSurface = deployable?.FailIfNotSurface ?? true;

        // Получаем сетку
        var gridUid = _xform.GetGrid(ignore);
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        // Проверяем, находится ли цель на поверхности планеты (как в RangefinderSystem)
        if (failIfNotSurface && !HasComp<RMCPlanetComponent>(gridUid) && user != null)
        {
            _popup.PopupClient($"Цель не находится на поверхности планеты", ignore, user.Value, PopupType.SmallCaution);
            return true;
        }

        // Проверяем каждый тайл в области
        var minTile = _map.TileIndicesFor((gridUid.Value, grid), new EntityCoordinates(gridUid.Value, area.BottomLeft));
        var maxTile = _map.TileIndicesFor((gridUid.Value, grid), new EntityCoordinates(gridUid.Value, area.TopRight));

        for (var x = minTile.X; x <= maxTile.X; x++)
        {
            for (var y = minTile.Y; y <= maxTile.Y; y++)
            {
                var tileIndices = new Vector2i(x, y);
                var enumerator = _map.GetAnchoredEntitiesEnumerator(gridUid.Value, grid, tileIndices);

                while (enumerator.MoveNext(out var ent))
                {
                    if (ent == ignore || (user != null && ent == user))
                        continue;

                    // Проверяем физические объекты с коллизией (как в AnchorableSystem)
                    if (TryComp<PhysicsComponent>(ent, out var physics) &&
                        physics.CanCollide &&
                        physics.Hard)
                    {
                        var name = MetaData(ent.Value).EntityName;
                        Logger.Info($"[Deploy] В тайле ({x},{y}) обнаружен физический объект: {ent} (имя: {name})");
                        found = true;
                        break;
                    }

                    // Проверяем структуры и другие блокирующие объекты (как в системах строительства)
                    if (HasComp<StrapComponent>(ent) ||
                        _tags.HasAnyTag(ent.Value, "Structure", "Airlock"))
                    {
                        var name = MetaData(ent.Value).EntityName;
                        Logger.Info($"[Deploy] В тайле ({x},{y}) обнаружена структура: {ent} (имя: {name})");
                        found = true;
                        break;
                    }

                    // Проверяем мобов в зоне (если включено)
                    if (HasComp<MobStateComponent>(ent))
                    {
                        var name = MetaData(ent.Value).EntityName;
                        Logger.Info($"[Deploy] В тайле ({x},{y}) обнаружен игрок/моб: {ent} (имя: {name})");
                        found = true;
                        break;
                    }
                }

                if (found) break;
            }
            if (found) break;
        }

        // Дополнительная проверка: ищем все сущности в области через EntityLookupSystem (как в RCDSystem)
        if (checkMobs)
        {
            var entitiesInArea = _lookup.GetEntitiesIntersecting(mapId, area);
            foreach (var ent in entitiesInArea)
            {
                if (ent == ignore || (user != null && ent == user))
                    continue;

                // Проверяем мобов (включая тех, кто может не быть закрепленными)
                if (TryComp<PhysicsComponent>(ent, out var physics) && (physics.Hard || physics.CanCollide))
                {
                    var name = MetaData(ent).EntityName;
                    Logger.Info($"[Deploy] Обнаружен физический объект: {ent} (имя: {name})");
                    found = true;
                    break;
                }
            }
        }

        // Проверяем заблокированные тайлы (как в системах строительства)
        for (var x = minTile.X; x <= maxTile.X; x++)
        {
            for (var y = minTile.Y; y <= maxTile.Y; y++)
            {
                var tileIndices = new Vector2i(x, y);
                var tileRef = _map.GetTileRef(gridUid.Value, grid, tileIndices);

                // Проверяем, является ли тайл космосом (как в TileNotBlocked)
                if (_turf.IsSpace(tileRef) && failIfNotSurface)
                {
                    Logger.Info($"[Deploy] Тайл ({x},{y}) является космосом");
                    found = true;
                    break;
                }

                // Проверяем, заблокирован ли тайл (как в TileNotBlocked)
                if (_turf.IsTileBlocked(tileRef, Content.Shared.Physics.CollisionGroup.Impassable))
                {
                    Logger.Info($"[Deploy] Тайл ({x},{y}) заблокирован");
                    found = true;
                    break;
                }


            }
            if (found) break;
        }

        if (found && user != null)
        {
            _popup.PopupClient("Зона развертывания заблокирована!", ignore, user.Value, PopupType.SmallCaution);
        }
        return found;
    }
}

[Serializable, NetSerializable]
public sealed partial class RMCDeployDoAfterEvent : SimpleDoAfterEvent { }
