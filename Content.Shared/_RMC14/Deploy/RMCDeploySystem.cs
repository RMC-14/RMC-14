using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Shared.Map;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Content.Shared._RMC14.Rules;
using Robust.Shared.Containers;
using Content.Shared.Item.ItemToggle.Components;
using System.Numerics;
using Content.Shared.Foldable;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;
using Content.Shared.Destructible;
using Content.Shared.Buckle.Components;
using Content.Shared.Buckle;
using Content.Shared.Storage.EntitySystems;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared._RMC14.Xenonids.Spray;
using Robust.Shared.Audio.Systems;

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
    [Dependency] private readonly FoldableSystem _foldable = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private List<EntityUid> _toDelete = [];

    public override void Initialize()
    {
        base.Initialize();
        // Reactions to attempts to deploy
        SubscribeLocalEvent<RMCDeployableComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCDeployableComponent, RMCDeployDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<RMCDeployableComponent, DoAfterAttemptEvent<RMCDeployDoAfterEvent>>(OnDeployDoAfterAttempt);
        // Track destruction of deployed entities
        SubscribeLocalEvent<RMCDeployedEntityComponent, ComponentShutdown>(OnDeployedEntityShutdown);
        SubscribeLocalEvent<RMCDeployableComponent, ComponentShutdown>(OnDeployableShutdown);
        // React to collapse attempt using a tool
        SubscribeLocalEvent<RMCDeployedEntityComponent, InteractUsingEvent>(OnParentalCollapseInteractUsing);
        SubscribeLocalEvent<RMCDeployedEntityComponent, RMCParentalCollapseDoAfterEvent>(OnParentalCollapseDoAfter);
        SubscribeLocalEvent<RMCDeployableComponent, ExaminedEvent>(OnDeployableExamined);
        SubscribeLocalEvent<RMCDeployedEntityComponent, ExaminedEvent>(OnDeployedExamined);
    }

    /// <summary>
    /// Handles the use-in-hand event for deployable entities.
    /// </summary>
    private void OnUseInHand(Entity<RMCDeployableComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryStartDeploy(ent, args.User);
    }

    /// <summary>
    /// Attempts to start the deploy process for a deployable entity.
    /// </summary>
    private void TryStartDeploy(Entity<RMCDeployableComponent> ent, EntityUid user)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;

        if (HasAnyAcid(uid))
        {
            _popup.PopupClient(Loc.GetString("rmc-deploy-popup-acid", ("entity", ent)), uid, user, PopupType.SmallCaution);
            return;
        }

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
            AttemptFrequency = AttemptFrequency.EveryTick
        };

        var started = _doAfter.TryStartDoAfter(doAfter);
        if (started)
        {
            _popup.PopupClient(Loc.GetString("rmc-deploy-popup-start"), ent.Owner, user, PopupType.Small);

            // Sending an event to display the deployment area on the client
            if (_netManager.IsServer)
            {
                ent.Comp.CurrentDeployUser = user;
                Dirty(ent);
                var showEvent = new RMCShowDeployAreaEvent(
                    area,
                    Color.Blue
                );
                RaiseNetworkEvent(showEvent, user);
            }
        }
    }

    /// <summary>
    /// Handles the completion of the deploy do-after event. Deploys setups if not cancelled.
    /// </summary>
    private void OnDoAfter(Entity<RMCDeployableComponent> ent, ref RMCDeployDoAfterEvent ev)
    {
        if (_netManager.IsClient)
            return;

        if (ev.Cancelled || ev.Handled)
        {
            ent.Comp.CurrentDeployUser = null;
            Dirty(ent);
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

        // Perform deployment
        DeploySetups(ent, areaCenter, user);

        if (ent.Comp.DeploySound != null)
            _audio.PlayPvs(ent.Comp.DeploySound, user);

        ent.Comp.CurrentDeployUser = null;
        Dirty(ent);
        RaiseNetworkEvent(new RMCHideDeployAreaEvent(), ev.Args.User);
    }

    /// <summary>
    /// Deploys setups, considering the possibility of redeploying existing entities.
    /// </summary>
    private void DeploySetups(Entity<RMCDeployableComponent> ent, Vector2 areaCenter, EntityUid user)
    {
        if (_netManager.IsClient)
            return;

        // Ensure the original entity has a ContainerManagerComponent
        EnsureComp<ContainerManagerComponent>(ent.Owner);

        // Check if the original entity has a container with previously deployed entities
        if (_container.TryGetContainer(ent.Owner, "storage", out var originalStorage) && originalStorage.ContainedEntities.Count > 0)
        {
            // Redeployment - extract existing entities
            RedeployExistingEntities(ent, areaCenter, originalStorage);
        }
        else
        {
            // First deployment - create all setups
            DeployAllSetups(ent, areaCenter, user);
        }
    }

    /// <summary>
    /// Redeploys existing entities from the container.
    /// </summary>
    private void RedeployExistingEntities(Entity<RMCDeployableComponent> ent, Vector2 areaCenter, BaseContainer originalStorage)
    {
        if (_netManager.IsClient)
            return;

        EntityUid? storageEntity = null;

        // We deploy only those entities that are in the container
        foreach (var containedEntity in originalStorage.ContainedEntities.ToList())
        {
            if (!TryComp<RMCDeployedEntityComponent>(containedEntity, out var deployedComp))
                continue;

            var setupIndex = deployedComp.SetupIndex;
            if (setupIndex < 0 || setupIndex >= ent.Comp.DeploySetups.Count)
                continue;

            var setup = ent.Comp.DeploySetups[setupIndex];

            // Skip setups marked as NeverRedeployableSetup (shouldn't get here, just in case)
            if (setup.NeverRedeployableSetup)
                continue;

            // Remove the entity from the container
            _container.Remove(containedEntity, originalStorage);

            // Unfold the entity if it has FoldableComponent using force method
            if (TryComp<FoldableComponent>(containedEntity, out var foldableComp))
                _foldable.SetFolded(containedEntity, foldableComp, false); // Use force method to bypass all safety checks

            // Update position and rotation
            var spawnPos = areaCenter + setup.Offset;
            _xform.SetWorldPosition(containedEntity, spawnPos);
            _xform.SetWorldRotation(containedEntity, Angle.FromDegrees(setup.Angle));

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
        }

        // Place the original entity in the storage container
        if (storageEntity != null)
        {
            var container = _container.EnsureContainer<Container>(storageEntity.Value, "storage");
            if (!_container.Insert(ent.Owner, container))
            {
                Log.Error($"Failed to place original entity {ent.Owner} in container 'storage' of entity {storageEntity.Value}");
            }
        }
    }

    /// <summary>
    /// Deploys all setups (first deployment).
    /// </summary>
    private void DeployAllSetups(Entity<RMCDeployableComponent> ent, Vector2 areaCenter, EntityUid user)
    {
        if (_netManager.IsClient)
            return;

        EntityUid? storageEntity = null;

        foreach (var (setup, i) in ent.Comp.DeploySetups.Select((s, idx) => (s, idx)))
        {
            var spawnPos = areaCenter + setup.Offset;
            Log.Debug($"RMCDeploySystem: Spawning entity {setup.Prototype} at position {spawnPos}");
            var spawned = Spawn(setup.Prototype, new MapCoordinates(spawnPos, _xform.GetMapId(user)));

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

            // Add RMCDeployedEntityComponent for tracking
            var childComp = EnsureComp<RMCDeployedEntityComponent>(spawned);
            childComp.OriginalEntity = ent.Owner;
            childComp.SetupIndex = i;
            Dirty(spawned, childComp);

            if (setup.StorageOriginalEntity && storageEntity == null)
                storageEntity = spawned;
        }

        // Place the original entity inside the storageEntity container, if it exists
        if (storageEntity != null)
        {
            var container = _container.EnsureContainer<Container>(storageEntity.Value, "storage");
            if (!_container.Insert(ent.Owner, container))
                Log.Error($"RMCDeploySystem: Failed to place original entity {ent.Owner} in container 'storage' of entity {storageEntity.Value}");
        }
        else
        {
            Log.Error("RMCDeploySystem: Original entity with StorageOriginalEntity not found for placement");
        }
    }

    /// <summary>
    /// Handles the DoAfter attempt event for deployable entities, checking if the area is blocked.
    /// </summary>
    private void OnDeployDoAfterAttempt(Entity<RMCDeployableComponent> ent, ref DoAfterAttemptEvent<RMCDeployDoAfterEvent> args)
    {
        var comp = ent.Comp;
        var uid = ent.Owner;
        var user = args.Event.User;
        var area = args.Event.Area;

        if (HasAnyAcid(uid))
        {
            _popup.PopupEntity(Loc.GetString("rmc-deploy-popup-acid", ("entity", ent)), user, user, PopupType.SmallCaution);
            args.Cancel();
            return;
        }

        if (comp.AreaBlockedCheck && IsAreaBlocked(area, uid, user, ent))
        {
            _popup.PopupEntity(Loc.GetString("rmc-deploy-popup-blocked"), user, user, PopupType.SmallCaution);
            args.Cancel();
        }
    }

    /// <summary>
    /// Checks if the deployment area is blocked by any physical objects.
    /// </summary>
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

    /// <summary>
    /// Checks if the deployment is allowed on the current surface (e.g., planet).
    /// </summary>
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

    /// <summary>
    /// Called when a deployed child entity is being deleted or its component is removed.
    /// Handles cleanup and possible deletion of related entities.
    /// </summary>
    private void OnDeployedEntityShutdown(Entity<RMCDeployedEntityComponent> ent, ref ComponentShutdown args)
    {
        if (_netManager.IsClient)
            return;

        // If already in shutdown, skip further processing
        if (ent.Comp.InShutdown)
            return;

        ent.Comp.InShutdown = true;
        Dirty(ent.Owner, ent.Comp);

        // Try to get the original entity
        if (!Exists(ent.Comp.OriginalEntity))
            return;

        if (!TryComp<RMCDeployableComponent>(ent.Comp.OriginalEntity, out var origComp))
            return;

        if (origComp is not null)
        {
            // Check if this was a ReactiveParentalSetup
            var setup = origComp.DeploySetups[ent.Comp.SetupIndex];
            if (setup.Mode == RMCDeploySetupMode.ReactiveParental)
            {
                // First, collect all entities to delete, then delete them outside the enumeration to avoid reentrancy and collection modification issues.
                var toDelete = new List<EntityUid>();
                var enumerator = EntityQueryEnumerator<RMCDeployedEntityComponent>();
                while (enumerator.MoveNext(out var entity, out var childComp))
                {
                    if (childComp.OriginalEntity != ent.Comp.OriginalEntity)
                        continue;
                    if (childComp.SetupIndex == ent.Comp.SetupIndex)
                        continue;
                    var mode = origComp.DeploySetups[childComp.SetupIndex].Mode;
                    if (mode == RMCDeploySetupMode.ReactiveParental || mode == RMCDeploySetupMode.Reactive)
                        toDelete.Add(entity);
                }
                foreach (var entity in toDelete)
                {
                    _destructible.DestroyEntity(entity);
                }
            }
        }
    }

    /// <summary>
    /// Called when a original entity is being deleted or its component is removed.
    /// Handles cleanup and possible deletion of related entities.
    /// </summary>
    private void OnDeployableShutdown(Entity<RMCDeployableComponent> ent, ref ComponentShutdown args)
    {
        if (_netManager.IsClient)
            return;

        if (ent.Comp.CurrentDeployUser != null)
            RaiseNetworkEvent(new RMCHideDeployAreaEvent(), ent.Comp.CurrentDeployUser.Value);

        _toDelete.Clear();
        var enumerator = EntityQueryEnumerator<RMCDeployedEntityComponent>();
        while (enumerator.MoveNext(out var entity, out var childComp))
        {
            if (childComp.OriginalEntity != ent.Owner)
                continue;
            if (childComp.SetupIndex < 0 || childComp.SetupIndex >= ent.Comp.DeploySetups.Count)
                continue;
            var setup = ent.Comp.DeploySetups[childComp.SetupIndex];
            if (setup.StorageOriginalEntity) //it is already stored inside the entity with such a flag, the entity itself will be deleted soon after that
            {
                childComp.InShutdown = true; // this will really work only in the process of deleting the entity that stores the original entity, in other cases it does not matter
                Dirty(entity, childComp);
                continue;
            }

            if (setup.Mode == RMCDeploySetupMode.ReactiveParental || setup.Mode == RMCDeploySetupMode.Reactive)
            {
                if (childComp.InShutdown)
                    continue;

                childComp.InShutdown = true;
                Dirty(entity, childComp);
                _toDelete.Add(entity);
            }
        }
        foreach (var entity in _toDelete)
        {
            _destructible.DestroyEntity(entity);
        }
    }

    /// <summary>
    /// Handles the attempt to collapse a deployed entity using a tool.
    /// </summary>
    private void OnParentalCollapseInteractUsing(Entity<RMCDeployedEntityComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        // Check if this entity is from a ReactiveParentalSetup
        if (!TryComp<RMCDeployableComponent>(ent.Comp.OriginalEntity, out var deployable))
            return;
        var setup = deployable.DeploySetups[ent.Comp.SetupIndex];
        if (setup.Mode != RMCDeploySetupMode.ReactiveParental)
            return;

        // Check if deployable has CollapseToolPrototype specified
        if (deployable.CollapseToolPrototype == null)
            return;

        // Check if the used item is the required tool
        if (!TryComp(args.Used, out MetaDataComponent? usedMeta) ||
            usedMeta.EntityPrototype == null ||
            usedMeta.EntityPrototype.ID != deployable.CollapseToolPrototype.Value)
            return;

        // If the tool has ItemToggleComponent, it must be activated (e.g., shovel)
        if (TryComp(args.Used, out ItemToggleComponent? toggle) && !toggle.Activated)
            return;

        var doAfter = new DoAfterArgs(_entMan, args.User, TimeSpan.FromSeconds(deployable.CollapseTime), new RMCParentalCollapseDoAfterEvent(), ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            NeedHand = true
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _popup.PopupClient(Loc.GetString("rmc-deployable-collapse-start"), args.User, args.User, PopupType.Small);

    }

    /// <summary>
    /// Handles the completion of the collapse do-after event for a deployed entity.
    /// Moves all child entities to the original entity's storage and restores the original entity.
    /// </summary>
    private void OnParentalCollapseDoAfter(Entity<RMCDeployedEntityComponent> ent, ref RMCParentalCollapseDoAfterEvent ev)
    {
        if (_netManager.IsClient)
            return;

        if (ev.Cancelled || ev.Handled)
            return;

        ev.Handled = true;

        var comp = ent.Comp;
        var user = ev.Args.User;

        // Get the original entity
        if (!TryComp(comp.OriginalEntity, out RMCDeployableComponent? deployable))
            return;

        // 1. Find the ReactiveParental entity whose storage contains the original entity
        EntityUid? reactiveParentalWithOriginal = null;
        var reactiveParentalEnumerator = EntityQueryEnumerator<RMCDeployedEntityComponent>();
        while (reactiveParentalEnumerator.MoveNext(out var reactiveParentalUid, out var reactiveParentalComp))
        {
            if (reactiveParentalComp.OriginalEntity != comp.OriginalEntity)
                continue;
            // Check if this is a ReactiveParentalSetup
            if (!TryComp(comp.OriginalEntity, out RMCDeployableComponent? origDeployable))
                continue;
            var setup = origDeployable.DeploySetups[reactiveParentalComp.SetupIndex];
            if (setup.Mode != RMCDeploySetupMode.ReactiveParental)
                continue;
            // Check storage
            if (_container.TryGetContainer(reactiveParentalUid, "storage", out var storage) && storage.Contains(comp.OriginalEntity))
            {
                reactiveParentalWithOriginal = reactiveParentalUid;
                break;
            }
        }
        // Extract the original entity only if found
        if (reactiveParentalWithOriginal != null)
        {
            if (_container.TryGetContainer(reactiveParentalWithOriginal.Value, "storage", out var storage) && storage.Contains(comp.OriginalEntity))
            {
                _container.Remove(comp.OriginalEntity, storage);
                // Place original entity at user's position
                var userCoords = _xform.GetWorldPosition(user);
                _xform.SetWorldPosition(comp.OriginalEntity, userCoords);
            }


            // 2. Move all child deployed entities (except NeverRedeployableSetup) to the storage container of the original entity
            var origStorage = _container.EnsureContainer<Container>(comp.OriginalEntity, "storage");
            var enumerator = EntityQueryEnumerator<RMCDeployedEntityComponent>();
            while (enumerator.MoveNext(out var childUid, out var childComp))
            {
                if (childComp.OriginalEntity != comp.OriginalEntity)
                    continue;
                if (childComp.SetupIndex < 0 || childComp.SetupIndex >= deployable.DeploySetups.Count)
                    continue;
                // Skip setups marked as NeverRedeployableSetup
                var childSetup = deployable.DeploySetups[childComp.SetupIndex];
                if (childSetup.NeverRedeployableSetup)
                    continue;
                // Do not add the original entity itself
                if (childUid == comp.OriginalEntity)
                    continue;

                // Prevents abuse when folding entities in cabinets, etc.
                _entityStorage.EmptyContents(childUid);

                // Unbuckle all entities strapped to the child entity
                TryUnbuckleAll(childUid);

                // Fold the entity if it has FoldableComponent (otherwise it won't fit in the container)
                if (TryComp<FoldableComponent>(childUid, out var foldableComp))
                    _foldable.SetFolded(childUid, foldableComp, true);

                // Add to the storage container of the original entity
                _container.Insert(childUid, origStorage);
            }

            if (deployable.CollapseSound != null)
                _audio.PlayPvs(deployable.CollapseSound, user);
        }
    }

    /// <summary>
    /// Method for preventing players from getting caught in a collapsed entity.
    /// </summary>
    private void TryUnbuckleAll(EntityUid entity)
    {
        if (!TryComp<StrapComponent>(entity, out var strap) || strap.BuckledEntities.Count == 0)
            return;

        foreach (var buckled in strap.BuckledEntities.ToArray())
        {
            _buckle.Unbuckle((buckled, CompOrNull<BuckleComponent>(buckled)), null);
        }
    }

    private bool HasAnyAcid(EntityUid uid)
    {
        return HasComp<TimedCorrodingComponent>(uid)
               || HasComp<DamageableCorrodingComponent>(uid)
               || HasComp<SprayAcidedComponent>(uid);
    }

    /// <summary>
    /// Adds a usage hint to items with RMCDeployableComponent when examined.
    /// </summary>
    private void OnDeployableExamined(Entity<RMCDeployableComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("rmc-deployable-examine-hint"));
    }

    /// <summary>
    /// Adds a collapse tool usage hint to deployed entities from ReactiveParental setups when examined.
    /// </summary>
    private void OnDeployedExamined(Entity<RMCDeployedEntityComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp(ent.Comp.OriginalEntity, out RMCDeployableComponent? deployable))
            return;
        if (ent.Comp.SetupIndex < 0 || ent.Comp.SetupIndex >= deployable.DeploySetups.Count)
            return;
        var setup = deployable.DeploySetups[ent.Comp.SetupIndex];
        if (setup.Mode != RMCDeploySetupMode.ReactiveParental)
            return;
        if (deployable.CollapseToolPrototype is { } toolProto &&
            _prototypeManager.TryIndex<EntityPrototype>(toolProto, out var proto))
        {
            var toolName = proto.Name;
            args.PushMarkup(Loc.GetString("rmc-deployed-collapse-hint", ("tool", toolName)));
        }
    }
}

