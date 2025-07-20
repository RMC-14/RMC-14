using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Shared.Map;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Content.Shared.Buckle.Components;
using Content.Shared._RMC14.Rules;
using Robust.Shared.Containers;
using Content.Shared.Item.ItemToggle.Components;
using System.Numerics;
using Content.Shared.Foldable;

namespace Content.Shared._RMC14.Deploy;

public sealed class RMCDeploySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly Tag.TagSystem _tags = default!;
    [Dependency] private readonly FoldableSystem _foldable = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCDeployableComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCDeployableComponent, RMCDeployDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<RMCDeployableComponent, DoAfterAttemptEvent<RMCDeployDoAfterEvent>>(OnDeployDoAfterAttempt);
        // Track destruction of deployed entities
        SubscribeLocalEvent<RMCSharedDeployedEntityComponent, ComponentShutdown>(OnDeployedEntityShutdown);
        // Реакция на попытку сворачивания через инструмент
        SubscribeLocalEvent<RMCSharedDeployedEntityComponent, InteractUsingEvent>(OnParentalCollapseInteractUsing);
        SubscribeLocalEvent<RMCSharedDeployedEntityComponent, RMCParentalCollapseDoAfterEvent>(OnParentalCollapseDoAfter);
    }

    private void OnUseInHand(Entity<RMCDeployableComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryStartDeploy(ent, args.User);
    }

    private void TryStartDeploy(Entity<RMCDeployableComponent> ent, EntityUid user)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;
        // Get the grid and the grid component
        var gridUid = _xform.GetGrid(user);
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        if (comp.FailIfNotSurface && !CheckSurface(gridUid.Value, uid, user))
            return;

        // Get the player's world position
        var userWorldPos = _xform.GetWorldPosition(user);
        // Tile indices under player
        var tileIndices = _map.WorldToTile(gridUid.Value, grid, userWorldPos);
        // Center of this tile
        var areaCenter = _map.TileCenterToVector(gridUid.Value, grid, tileIndices);

        var transform = new Robust.Shared.Physics.Transform(areaCenter, 0);
        var area = comp.DeployArea.ComputeAABB(transform, 0);

        // Check if the area is blocked (if not disabled)
        if (comp.AreaBlockedCheck && IsAreaBlocked(area, uid, user, ent))
            return;

        var doAfter = new DoAfterArgs(_entMan, user, comp.DeployTime, new RMCDeployDoAfterEvent(area), uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            NeedHand = true,
            AttemptFrequency = comp.AreaBlockedCheck ? AttemptFrequency.EveryTick : AttemptFrequency.Never
        };

        var started = _doAfter.TryStartDoAfter(doAfter);
        if (started)
        {
            _popup.PopupClient(Loc.GetString("rmc-deploy-popup-start"), ent.Owner, user, PopupType.Small);

            // Sending an event to display the deployment area on the client
            if (_netManager.IsServer)
            {
                var showEvent = new RMCShowDeployAreaEvent(
                    area,
                    Color.Blue
                );
                RaiseNetworkEvent(showEvent, user);
            }
        }

    }

    private void OnDoAfter(Entity<RMCDeployableComponent> ent, ref RMCDeployDoAfterEvent ev)
    {
        if (_netManager.IsClient)
            return;

        if (ev.Cancelled || ev.Handled)
        {
            RaiseNetworkEvent(new RMCHideDeployAreaEvent(), ev.Args.User);
            return;
        }

        ev.Handled = true;


        var user = ev.Args.User;
        // Get the grid and the grid component
        var gridUid = _xform.GetGrid(user);
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        // Get the player's world position
        var userWorldPos = _xform.GetWorldPosition(user);
        // Tile indices under player
        var tileIndices = _map.WorldToTile(gridUid.Value, grid, userWorldPos);
        // The center of this tile (for calculating the zone)
        var tileCenter = _map.TileCenterToVector(gridUid.Value, grid, tileIndices);

        // Build transform and zone relative to tileCenter
        var areaTransform = new Robust.Shared.Physics.Transform(tileCenter, 0);
        var area = ent.Comp.DeployArea.ComputeAABB(areaTransform, 0);
        var areaCenter = area.Center;

        // Выполняем развертывание
        DeploySetups(ent, areaCenter, user);

        RaiseNetworkEvent(new RMCHideDeployAreaEvent(), ev.Args.User);
    }

    /// <summary>
    /// Развертывает сетапы с учетом возможного повторного развертывания существующих сущностей
    /// </summary>
    private void DeploySetups(Entity<RMCDeployableComponent> ent, Vector2 areaCenter, EntityUid user)
    {
        // Убеждаемся, что у оригинальной сущности есть ContainerManagerComponent
        EntityManager.EnsureComponent<ContainerManagerComponent>(ent.Owner);

        // Проверяем, есть ли в оригинальной сущности контейнер с ранее развернутыми сущностями
        if (_container.TryGetContainer(ent.Owner, "storage", out var originalStorage) && originalStorage.ContainedEntities.Count > 0)
        {
            // Повторное развертывание - извлекаем существующие сущности
            RedeployExistingEntities(ent, areaCenter, originalStorage);
        }
        else
        {
            // Первое развертывание - создаем все сетапы
            DeployAllSetups(ent, areaCenter, user);
        }
    }

    /// <summary>
    /// Повторно развертывает существующие сущности из контейнера
    /// </summary>
    private void RedeployExistingEntities(Entity<RMCDeployableComponent> ent, Vector2 areaCenter, BaseContainer originalStorage)
    {
        EntityUid? storageEntity = null;

        // Разворачиваем только те сущности, которые есть в контейнере
        foreach (var containedEntity in originalStorage.ContainedEntities.ToList())
        {
            if (!TryComp<RMCSharedDeployedEntityComponent>(containedEntity, out var deployedComp))
                continue;

            var setupIndex = deployedComp.SetupIndex;
            if (setupIndex >= ent.Comp.DeploySetups.Count)
                continue;

            var setup = ent.Comp.DeploySetups[setupIndex];

            // Пропускаем сетапы, помеченные как NeverRedeployableSetup (они бы сюда и так не попали, перестраховка)
            if (setup.NeverRedeployableSetup)
                continue;

            // Извлекаем сущность из контейнера
            _container.Remove(containedEntity, originalStorage);

            // Обновляем позицию и поворот
            var spawnPos = areaCenter + setup.Offset;
            _xform.SetWorldPosition(containedEntity, spawnPos);
            _xform.SetWorldRotation(containedEntity, Angle.FromDegrees(setup.Angle));

            // Разворачиваем сущность, если у неё есть FoldableComponent
            if (TryComp<FoldableComponent>(containedEntity, out var foldableComp))
            {
                _foldable.TrySetFolded(containedEntity, foldableComp, false);
            }

            if (setup.Anchor)
            {
                var xform = Transform(containedEntity);
                if (!xform.Anchored)
                {
                    _xform.AnchorEntity((containedEntity, xform));
                }
            }

            if (setup.StorageOriginalEntity && storageEntity == null)
                storageEntity = containedEntity;

            Logger.Info($"[Deploy] Повторно развернута существующая сущность {containedEntity} для сетапа {setupIndex}");
        }

        // Помещаем оригинальную сущность в storage контейнер
        if (storageEntity != null)
        {
            var container = _container.EnsureContainer<Container>(storageEntity.Value, "storage");
            if (!_container.Insert(ent.Owner, container))
            {
                Logger.GetSawmill("entity").Error($"Не удалось поместить оригинальную сущность {ent.Owner} в контейнер 'storage' сущности {storageEntity.Value}");
            }
        }
    }

    /// <summary>
    /// Развертывает все сетапы (первое развертывание)
    /// </summary>
    private void DeployAllSetups(Entity<RMCDeployableComponent> ent, Vector2 areaCenter, EntityUid user)
    {
        EntityUid? storageEntity = null;

        foreach (var (setup, i) in ent.Comp.DeploySetups.Select((s, idx) => (s, idx)))
        {
            var spawnPos = areaCenter + setup.Offset;
            var spawned = EntityManager.SpawnEntity(setup.Prototype, new MapCoordinates(spawnPos, _xform.GetMapId(user)));

            _xform.SetWorldPosition(spawned, spawnPos);
            _xform.SetWorldRotation(spawned, Angle.FromDegrees(setup.Angle));

            if (setup.Anchor)
            {
                var xform = Transform(spawned);
                if (!xform.Anchored)
                {
                    _xform.AnchorEntity((spawned, xform));
                }
            }

            // Add RMCSharedDeployedEntityComponent for tracking
            var childComp = EntityManager.EnsureComponent<RMCSharedDeployedEntityComponent>(spawned);
            childComp.OriginalEntity = ent.Owner;
            childComp.SetupIndex = i;
            Dirty(spawned, childComp);

            if (setup.StorageOriginalEntity && storageEntity == null)
                storageEntity = spawned;

            Logger.Info($"[Deploy] Создана новая сущность {spawned} для сетапа {i}");
        }

        // Помещаем оригинальную сущность внутрь контейнера storageEntity, если он есть
        if (storageEntity != null)
        {
            var container = _container.EnsureContainer<Container>(storageEntity.Value, "storage");
            if (!_container.Insert(ent.Owner, container))
            {
                Logger.GetSawmill("entity").Error($"Не удалось поместить оригинальную сущность {ent.Owner} в контейнер 'storage' сущности {storageEntity.Value}");
            }
        }
        else
        {
            Logger.GetSawmill("entity").Error("Не найдена сущность с StorageOriginalEntity для помещения оригинала");
        }
    }

    private void OnDeployDoAfterAttempt(Entity<RMCDeployableComponent> ent, ref DoAfterAttemptEvent<RMCDeployDoAfterEvent> args)
    {
        var comp = ent.Comp;
        var uid = ent.Owner;
        var user = args.Event.User;
        var area = args.Event.Area;
        if (comp.AreaBlockedCheck && IsAreaBlocked(area, uid, user, ent))
        {
            _popup.PopupEntity(Loc.GetString("rmc-deploy-popup-blocked"), user, user, PopupType.SmallCaution);
            args.Cancel();
        }
    }

    private bool IsAreaBlocked(Box2 area, EntityUid ignore, EntityUid? user = null, Entity<RMCDeployableComponent>? ent = null)
    {
        var deployable = ent?.Comp;
        var mapId = _xform.GetMapId(ignore);
        var found = false;
        var failIfNotSurface = deployable?.FailIfNotSurface ?? true;

        // Get the grid
        var gridUid = _xform.GetGrid(ignore);
        if (!HasComp<MapGridComponent>(gridUid))
            return false;

        if (failIfNotSurface && gridUid.HasValue && !CheckSurface(gridUid.Value, ignore, user))
            return true;

        // Check all entities that actually intersect with the area
        var entitiesInArea = _lookup.GetEntitiesIntersecting(mapId, area);
        foreach (var entId in entitiesInArea)
        {
            if (entId == ignore || (user != null && entId == user))
                continue;

            if (TryComp<PhysicsComponent>(entId, out var physics) && (physics.CanCollide || physics.Hard))
            {
                var name = MetaData(entId).EntityName;
                Logger.Info($"[Deploy] В области обнаружен физический объект: {entId} (имя: {name})");
                found = true;
                break;
            }
        }

        if (found && user != null && _netManager.IsClient)
        {
            _popup.PopupClient(Loc.GetString("rmc-deploy-popup-blocked"), user.Value, user.Value, PopupType.SmallCaution);
        }

        return found;
    }

    private bool CheckSurface(EntityUid gridUid, EntityUid ignore, EntityUid? user)
    {
        if (!HasComp<RMCPlanetComponent>(gridUid))
        {
            if (user != null && _netManager.IsClient)
                _popup.PopupClient(Loc.GetString("rmc-deploy-popup-surface"), ignore, user.Value, PopupType.SmallCaution);

            return false;
        }
        return true;
    }

    // Called when a deployed child entity is being deleted or its component is removed
    private void OnDeployedEntityShutdown(EntityUid uid, RMCSharedDeployedEntityComponent comp, ComponentShutdown args)
    {
        // If already in shutdown, skip further processing
        if (comp.InShutdown)
            return;
        comp.InShutdown = true;
        Dirty(uid, comp);

        // Try to get the original entity
        if (!EntityManager.EntityExists(comp.OriginalEntity))
            return;

        if (!EntityManager.TryGetComponent(comp.OriginalEntity, out RMCDeployableComponent? origComp))
            return;

        if (origComp is not null)
        {
            // Проверяем, был ли это ReactiveParentalSetup
            var setup = origComp.DeploySetups[comp.SetupIndex];
            if (setup.Mode == RMCDeploySetupMode.ReactiveParental)
            {
                // First, collect all entities to delete, then delete them outside the enumeration to avoid reentrancy and collection modification issues.
                var toDelete = new List<EntityUid>();
                var enumerator = EntityManager.EntityQueryEnumerator<RMCSharedDeployedEntityComponent>();
                while (enumerator.MoveNext(out var entity, out var childComp))
                {
                    if (childComp.OriginalEntity != comp.OriginalEntity)
                        continue;
                    if (childComp.SetupIndex == comp.SetupIndex)
                        continue;
                    var mode = origComp.DeploySetups[childComp.SetupIndex].Mode;
                    if (mode == RMCDeploySetupMode.ReactiveParental || mode == RMCDeploySetupMode.Reactive)
                    {
                        toDelete.Add(entity);
                    }
                }
                foreach (var entity in toDelete)
                {
                    EntityManager.DeleteEntity(entity);
                }
            }
        }
    }

    private void OnParentalCollapseInteractUsing(EntityUid uid, RMCSharedDeployedEntityComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        // Проверяем, что эта сущность из ReactiveParentalSetup
        if (!EntityManager.TryGetComponent(comp.OriginalEntity, out RMCDeployableComponent? deployable))
            return;
        var setup = deployable.DeploySetups[comp.SetupIndex];
        if (setup.Mode != RMCDeploySetupMode.ReactiveParental)
            return;

        // Проверяем, что у deployable указан CollapseToolPrototype
        if (deployable.CollapseToolPrototype == null)
            return;

        // Проверяем, что используемый предмет — нужный инструмент
        if (!EntityManager.TryGetComponent(args.Used, out MetaDataComponent? usedMeta) ||
            usedMeta.EntityPrototype == null ||
            usedMeta.EntityPrototype.ID != deployable.CollapseToolPrototype.Value)
            return;

        // Если у инструмента есть ItemToggleComponent — он должен быть развернут (например лопата)
        if (EntityManager.TryGetComponent(args.Used, out ItemToggleComponent? toggle) && !toggle.Activated)
            return;

        var doAfter = new DoAfterArgs(_entMan, args.User, TimeSpan.FromSeconds(deployable.CollapseTime), new RMCParentalCollapseDoAfterEvent(), uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            NeedHand = true
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _popup.PopupClient("Вы начинаете сворачивание...", args.User, args.User, PopupType.Small);

    }

    private void OnParentalCollapseDoAfter(Entity<RMCSharedDeployedEntityComponent> ent, ref RMCParentalCollapseDoAfterEvent ev)
    {
        if (_netManager.IsClient)
            return;

        if (ev.Cancelled || ev.Handled)
            return;

        ev.Handled = true;

        var comp = ent.Comp;
        var user = ev.Args.User;

        // Get the original entity
        if (!EntityManager.TryGetComponent(comp.OriginalEntity, out RMCDeployableComponent? deployable))
            return;

        // 1. Найти ReactiveParental, в чьём storage лежит оригинальная сущность
        EntityUid? reactiveParentalWithOriginal = null;
        var reactiveParentalEnumerator = EntityManager.EntityQueryEnumerator<RMCSharedDeployedEntityComponent>();
        while (reactiveParentalEnumerator.MoveNext(out var reactiveParentalUid, out var reactiveParentalComp))
        {
            if (reactiveParentalComp.OriginalEntity != comp.OriginalEntity)
                continue;
            // Проверяем, что это ReactiveParentalSetup
            if (!EntityManager.TryGetComponent(comp.OriginalEntity, out RMCDeployableComponent? origDeployable))
                continue;
            var setup = origDeployable.DeploySetups[reactiveParentalComp.SetupIndex];
            if (setup.Mode != RMCDeploySetupMode.ReactiveParental)
                continue;
            // Проверяем storage
            if (_container.TryGetContainer(reactiveParentalUid, "storage", out var storage) && storage.Contains(comp.OriginalEntity))
            {
                reactiveParentalWithOriginal = reactiveParentalUid;
                break;
            }
        }
        // Вытаскиваем оригинал только если нашли
        if (reactiveParentalWithOriginal != null)
        {
            if (_container.TryGetContainer(reactiveParentalWithOriginal.Value, "storage", out var storage) && storage.Contains(comp.OriginalEntity))
            {
                _container.Remove(comp.OriginalEntity, storage);
                // Place original entity at user's position
                var userCoords = _xform.GetWorldPosition(user);
                _xform.SetWorldPosition(comp.OriginalEntity, userCoords);
            }


            // 2. Move all child deployed entities (кроме NeverRedeployableSetup) в storage контейнер оригинальной сущности
            var origStorage = _container.EnsureContainer<Container>(comp.OriginalEntity, "storage");
            var enumerator = EntityManager.EntityQueryEnumerator<RMCSharedDeployedEntityComponent>();
            while (enumerator.MoveNext(out var childUid, out var childComp))
            {
                if (childComp.OriginalEntity != comp.OriginalEntity)
                    continue;
                // Пропускаем сетапы, помеченные как NeverRedeployableSetup
                var childSetup = deployable.DeploySetups[childComp.SetupIndex];
                if (childSetup.NeverRedeployableSetup)
                    continue;
                // Не добавлять саму оригинальную сущность
                if (childUid == comp.OriginalEntity)
                    continue;

                // Сворачиваем сущность, если у неё есть FoldableComponent (без этого не попадет в контейнер лол)
                if (TryComp<FoldableComponent>(childUid, out var foldableComp))
                {
                    _foldable.TrySetFolded(childUid, foldableComp, true);
                }

                // Добавить в storage контейнер оригинальной сущности
                _container.Insert(childUid, origStorage);
            }
        }
    }
}

