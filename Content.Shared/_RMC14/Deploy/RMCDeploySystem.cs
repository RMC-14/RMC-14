using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Robust.Shared.Map;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Content.Shared.Buckle.Components;
using Content.Shared._RMC14.Rules;

namespace Content.Shared._RMC14.Deploy;

public sealed class RMCDeploySystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly Tag.TagSystem _tags = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCDeployableComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCDeployableComponent, RMCDeployDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<RMCDeployableComponent, DoAfterAttemptEvent<RMCDeployDoAfterEvent>>(OnDeployDoAfterAttempt);
        // Track destruction of deployed entities
        SubscribeLocalEvent<RMCDeployedEntityComponent, ComponentShutdown>(OnDeployedEntityShutdown);
    }

    private void OnUseInHand(Entity<RMCDeployableComponent> ent, ref UseInHandEvent args)
    {
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

        foreach (var (setup, i) in ent.Comp.DeploySetups.Select((s, idx) => (s, idx)))
        {
            if (ent.Comp.NonRedeployableSetups.Contains(i)) // Tracks indices of setups that should not be redeployed
                continue;

            var spawnPos = areaCenter + setup.Offset;
            var spawned = EntityManager.SpawnEntity(setup.Prototype, new MapCoordinates(spawnPos, _xform.GetMapId(user)));

            _xform.SetWorldPosition(spawned, spawnPos);
            _xform.SetWorldRotation(spawned, Angle.FromDegrees(setup.Angle));

            if (setup.Anchor)
            {
                var xform = Transform(spawned);
                // Anchor only if not already anchored and entity supports anchoring
                if (!xform.Anchored)
                {
                    _xform.AnchorEntity((spawned, xform));
                }
            }

            // Add RMCDeployedEntityComponent for tracking
            var childComp = EntityManager.EnsureComponent<RMCDeployedEntityComponent>(spawned);
            childComp.OriginalEntity = ent.Owner;
            childComp.SetupIndex = i;
            Dirty(spawned, childComp);

            // Если сетап одноразовый — сразу помечаем его как не подлежащий повторному разворачиванию
            if (setup.NeverRedeployableSetup)
            {
                ent.Comp.NonRedeployableSetups.Add(i);
                Dirty(ent.Owner, ent.Comp);
            }
        }

        RaiseNetworkEvent(new RMCHideDeployAreaEvent(), ev.Args.User);
        // Delete the original entity after deployment
        //EntityManager.DeleteEntity(ent.Owner);
    }

    private void OnDeployDoAfterAttempt(Entity<RMCDeployableComponent> ent, ref DoAfterAttemptEvent<RMCDeployDoAfterEvent> args)
    {
        var comp = ent.Comp;
        var uid = ent.Owner;
        var user = args.Event.User;
        var area = args.Event.Area;
        if (comp.AreaBlockedCheck && IsAreaBlocked(area, uid, user, ent))
        {
            args.Cancel();
            _popup.PopupClient(Loc.GetString("rmc-deploy-popup-blocked"), uid, user, PopupType.SmallCaution);
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

            if (HasComp<StrapComponent>(entId) || _tags.HasAnyTag(entId, "Structure", "Airlock"))
            {
                var name = MetaData(entId).EntityName;
                Logger.Info($"[Deploy] В области обнаружена структура: {entId} (имя: {name})");
                found = true;
                break;
            }
        }

        if (found && user != null)
        {
            _popup.PopupClient(Loc.GetString("rmc-deploy-popup-blocked"), ignore, user.Value, PopupType.SmallCaution);
        }

        return found;
    }

    private bool CheckSurface(EntityUid gridUid, EntityUid ignore, EntityUid? user)
    {
        if (!HasComp<RMCPlanetComponent>(gridUid))
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("rmc-deploy-popup-surface"), ignore, user.Value, PopupType.SmallCaution);
            return false;
        }
        return true;
    }

    // Called when a deployed child entity is being deleted or its component is removed
    private void OnDeployedEntityShutdown(EntityUid uid, RMCDeployedEntityComponent comp, ComponentShutdown args)
    {
        // If already in shutdown, skip further processing
        if (comp.InShutdown)
            return;
        comp.InShutdown = true;
        Dirty(uid, comp);

        // Try to get the original entity and mark the setup as destroyed
        if (!EntityManager.EntityExists(comp.OriginalEntity))
            return;

        if (!EntityManager.TryGetComponent(comp.OriginalEntity, out RMCDeployableComponent? origComp))
            return;

        if (origComp is not null)
        {
            origComp.NonRedeployableSetups.Add(comp.SetupIndex);
            Dirty(comp.OriginalEntity, origComp);

            // Проверяем, был ли это ParentalSetup
            var setup = origComp.DeploySetups[comp.SetupIndex];
            if (setup.ParentalSetup)
            {
                // First, collect all entities to delete, then delete them outside the enumeration to avoid reentrancy and collection modification issues.
                var toDelete = new List<EntityUid>();
                var enumerator = EntityManager.EntityQueryEnumerator<RMCDeployedEntityComponent>();
                while (enumerator.MoveNext(out var entity, out var childComp))
                {
                    if (childComp.OriginalEntity != comp.OriginalEntity)
                        continue;
                    if (childComp.SetupIndex == comp.SetupIndex)
                        continue;
                    if (origComp.DeploySetups[childComp.SetupIndex].ReactiveSetup)
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
}

