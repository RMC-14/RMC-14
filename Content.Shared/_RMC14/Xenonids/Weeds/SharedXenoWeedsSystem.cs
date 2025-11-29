using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.FloorResin;
using Content.Shared._RMC14.Xenonids.Construction.ResinHole;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Climbing.Components;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Maps;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Weeds;

public abstract class SharedXenoWeedsSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityManager _entities = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    private readonly HashSet<EntityUid> _toUpdate = new();
    private readonly HashSet<EntityUid> _intersecting = new();

    private EntityQuery<AffectableByWeedsComponent> _affectedQuery;
    private EntityQuery<XenoWeedsComponent> _weedsQuery;
    private EntityQuery<ResinSlowdownModifierComponent> _slowResinQuery;
    private EntityQuery<ResinSpeedupModifierComponent> _fastResinQuery;
    private EntityQuery<XenoComponent> _xenoQuery;
    private EntityQuery<BlockWeedsComponent> _blockWeedsQuery;
    private EntityQuery<HiveMemberComponent> _hiveQuery;

    public override void Initialize()
    {
        _affectedQuery = GetEntityQuery<AffectableByWeedsComponent>();
        _weedsQuery = GetEntityQuery<XenoWeedsComponent>();
        _slowResinQuery = GetEntityQuery<ResinSlowdownModifierComponent>();
        _fastResinQuery = GetEntityQuery<ResinSpeedupModifierComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();
        _blockWeedsQuery = GetEntityQuery<BlockWeedsComponent>();
        _hiveQuery = GetEntityQuery<HiveMemberComponent>();

        SubscribeLocalEvent<XenoWeedsComponent, AnchorStateChangedEvent>(OnWeedsAnchorChanged);
        SubscribeLocalEvent<XenoWeedsComponent, ComponentShutdown>(OnModifierShutdown);
        SubscribeLocalEvent<XenoWeedsComponent, EntityTerminatingEvent>(OnWeedsTerminating);
        SubscribeLocalEvent<XenoWeedsComponent, MapInitEvent>(OnWeedsMapInit);
        SubscribeLocalEvent<XenoWeedsComponent, StartCollideEvent>(OnWeedsStartCollide);
        SubscribeLocalEvent<XenoWeedsComponent, EndCollideEvent>(OnWeedsEndCollide);
        SubscribeLocalEvent<XenoWeedsComponent, ExaminedEvent>(OnWeedsExamined);

        SubscribeLocalEvent<XenoWallWeedsComponent, ComponentRemove>(OnWallWeedsRemove);
        SubscribeLocalEvent<XenoWallWeedsComponent, EntityTerminatingEvent>(OnWallWeedsRemove);

        SubscribeLocalEvent<XenoWeedableComponent, AnchorStateChangedEvent>(OnWeedableAnchorStateChanged);
        SubscribeLocalEvent<XenoWeedableComponent, ComponentRemove>(OnWeedableRemove);
        SubscribeLocalEvent<XenoWeedableComponent, EntityTerminatingEvent>(OnWeedableRemove);

        SubscribeLocalEvent<DamageOffWeedsComponent, MapInitEvent>(OnDamageOffWeedsMapInit);

        SubscribeLocalEvent<AffectableByWeedsComponent, RefreshMovementSpeedModifiersEvent>(WeedsRefreshPassiveSpeed);
        SubscribeLocalEvent<AffectableByWeedsComponent, XenoOvipositorChangedEvent>(WeedsOvipositorChanged);

        SubscribeLocalEvent<XenoWeedsSpreadingComponent, MapInitEvent>(OnSpreadingMapInit);

        SubscribeLocalEvent<ResinSlowdownModifierComponent, ComponentShutdown>(OnModifierShutdown);
        SubscribeLocalEvent<ResinSlowdownModifierComponent, StartCollideEvent>(OnResinSlowdownStartCollide);
        SubscribeLocalEvent<ResinSlowdownModifierComponent, EndCollideEvent>(OnResinSlowdownEndCollide);

        SubscribeLocalEvent<ResinSpeedupModifierComponent, ComponentShutdown>(OnModifierShutdown);
        SubscribeLocalEvent<ResinSpeedupModifierComponent, StartCollideEvent>(OnResinSpeedupStartCollide);
        SubscribeLocalEvent<ResinSpeedupModifierComponent, EndCollideEvent>(OnResinSpeedupEndCollide);

        SubscribeLocalEvent<ReplaceWeedSourceOnWeedingComponent, AfterEntityWeedingEvent>(OnWeedOver);

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
    }

    private void OnWeedsExamined(Entity<XenoWeedsComponent> weeds, ref ExaminedEvent args)
    {
        if (!HasComp<XenoComponent>(args.Examiner))
            return;

        if (weeds.Comp.FruitGrowthMultiplier == 1.0f)
            return;

        using (args.PushGroup(nameof(XenoWeedsComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-xeno-fruit-weed-boost", ("percent", (int)(weeds.Comp.FruitGrowthMultiplier * 100))));
        }

    }

    private void OnWeedsAnchorChanged(Entity<XenoWeedsComponent> weeds, ref AnchorStateChangedEvent args)
    {
        if (_net.IsServer && !args.Anchored)
            QueueDel(weeds);
    }

    private void OnModifierShutdown<T>(Entity<T> ent, ref ComponentShutdown args) where T : IComponent
    {
        if (!TryComp(ent, out PhysicsComponent? phys))
            return;

        _toUpdate.UnionWith(_physics.GetContactingEntities(ent, phys));
    }

    private void OnWeedsTerminating(Entity<XenoWeedsComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!ent.Comp.IsSource)
        {
            if (_weedsQuery.TryComp(ent.Comp.Source, out var weeds))
            {
                weeds.Spread.Remove(ent);
                Dirty(ent.Comp.Source.Value, weeds);
            }

            foreach (var weededEntity in ent.Comp.LocalWeeded)
            {
                _appearance.SetData(weededEntity, WeededEntityLayers.Layer, false);
            }

            return;
        }

        foreach (var spread in ent.Comp.Spread)
        {
            if (TerminatingOrDeleted(spread))
                continue;

            if (_weedsQuery.TryComp(spread, out var weeds))
            {
                weeds.Source = null;
                Dirty(spread, weeds);
            }

            var timed = EnsureComp<TimedDespawnComponent>(spread);
            var offset = _random.Next(ent.Comp.MinRandomDelete, ent.Comp.MaxRandomDelete);
            timed.Lifetime = (float) offset.TotalSeconds;
        }

        ent.Comp.Spread.Clear();
        Dirty(ent);
    }

    private void OnWeedsMapInit(Entity<XenoWeedsComponent> ent, ref MapInitEvent args)
    {
        foreach (var intersecting in _physics.GetEntitiesIntersectingBody(ent, (int) CollisionGroup.MobLayer))
        {
            if (_affectedQuery.TryComp(intersecting, out var affected) && !affected.OnXenoWeeds)
                _toUpdate.Add(intersecting);
        }
    }

    private void OnWeedsStartCollide(Entity<XenoWeedsComponent> ent, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (_affectedQuery.TryComp(other, out var affected) && !affected.OnXenoWeeds)
            _toUpdate.Add(other);
    }

    private void OnWeedsEndCollide(Entity<XenoWeedsComponent> ent, ref EndCollideEvent args)
    {
        var other = args.OtherEntity;
        if (_affectedQuery.TryComp(other, out var affected) && affected.OnXenoWeeds)
            _toUpdate.Add(other);
    }

    private void OnWallWeedsRemove<T>(Entity<XenoWallWeedsComponent> ent, ref T args)
    {
        if (!TryComp(ent.Comp.Weeds, out XenoWeedsComponent? weeds))
            return;

        weeds.Spread.Remove(ent);
        Dirty(ent.Comp.Weeds.Value, weeds);
    }

    private void OnWeedableAnchorStateChanged(Entity<XenoWeedableComponent> weedable, ref AnchorStateChangedEvent args)
    {
        if (_net.IsServer && !args.Anchored)
            QueueDel(weedable.Comp.Entity);
    }

    private void OnWeedableRemove<T>(Entity<XenoWeedableComponent> weedable, ref T args)
    {
        if (_net.IsServer && weedable.Comp.Entity != null)
        {
            QueueDel(weedable.Comp.Entity);
        }
    }

    private void OnDamageOffWeedsMapInit(Entity<DamageOffWeedsComponent> damage, ref MapInitEvent args)
    {
        damage.Comp.DamageAt = _timing.CurTime + damage.Comp.Every;
    }

    private void WeedsRefreshPassiveSpeed(Entity<AffectableByWeedsComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp<PhysicsComponent>(ent, out var physicsComponent))
            return;

        var speedWeeds = 0.0f;
        var speedResin = 0.0f;
        var isXeno = _xenoQuery.HasComp(ent);
        //Checks hive for applying slows now
        //Weed speedup only effects xenos, but slowdown does not hurt hive mems
        //Fast resin speedup only effect xenos, but sticky also doesn't hurt hive mems
        _hiveQuery.TryComp(ent, out var hive);

        var anyWeeds = false;
        var anySlowResin = false;
        var anyFastResin = false;
        var friendlyWeeds = false;
        var entriesResin = 0;
        var entriesWeeds = 0;

        _intersecting.Clear();
        _physics.GetContactingEntities((ent, physicsComponent), _intersecting);

        if (TryComp(ent, out TransformComponent? transform) &&
            transform.Anchored)
        {
            var anchoredQuery = _rmcMap.GetAnchoredEntitiesEnumerator(ent);
            while (anchoredQuery.MoveNext(out var anchored))
            {
                _intersecting.Add(anchored);
            }
        }

        foreach (var contacting in _intersecting)
        {
            if (_slowResinQuery.TryComp(contacting, out var slowResin))
            {
                if (hive == null || !_hive.IsMember(contacting, hive.Hive))
                {
                    if (HasComp<RMCArmorSpeedTierUserComponent>(contacting))
                        speedResin += slowResin.OutsiderSpeedModifierArmor;
                    else
                        speedResin += slowResin.OutsiderSpeedModifier;

                    entriesResin++;
                }
                anySlowResin = true;
                continue;
            }

            if (_fastResinQuery.TryComp(contacting, out var fastResin))
            {
                if (isXeno && hive != null && _hive.IsMember(contacting, hive.Hive))
                {
                    speedResin += fastResin.HiveSpeedModifier;
                    entriesResin++;
                }
                anyFastResin = true;
                continue;
            }

            if (!_weedsQuery.TryComp(contacting, out var weeds))
                continue;

            anyWeeds = true;

            if (isXeno && hive != null && _hive.IsMember(contacting, hive.Hive))
            {
                speedWeeds += weeds.SpeedMultiplierXeno;
                friendlyWeeds = true;
                entriesWeeds++;
            }
            else if (hive == null || !_hive.IsMember(contacting, hive.Hive))
            {
                if (HasComp<RMCArmorSpeedTierUserComponent>(contacting))
                    speedWeeds += weeds.SpeedMultiplierOutsiderArmor;
                else
                    speedWeeds += weeds.SpeedMultiplierOutsider;

                entriesWeeds++;
            }
        }

        if (!anyWeeds &&
            Transform(ent).Anchored &&
            _rmcMap.HasAnchoredEntityEnumerator<XenoWeedsComponent>(ent.Owner.ToCoordinates()))
        {
            anyWeeds = true;
        }
        //Resin + Weed Speedups stack, but resin + weed slowdowns do not
        var finalSpeed = 1.0f;
        if (entriesWeeds > 0)
            speedWeeds /= entriesWeeds;

        if (entriesResin > 0)
            speedResin /= entriesResin;

        //If Weeds is a speedup, let them stack, otherwise treat them as slowdownss
        if ((speedWeeds > 1 || speedResin > 1) && entriesResin > 0 && entriesWeeds > 0)
            finalSpeed = speedWeeds * speedResin;
        else if (entriesResin > 0)
            finalSpeed = speedResin;
        else if (entriesWeeds > 0)
            finalSpeed = speedWeeds;

        args.ModifySpeed(finalSpeed, finalSpeed);

        ent.Comp.OnXenoWeeds = anyWeeds;
        ent.Comp.OnFriendlyWeeds = friendlyWeeds;
        ent.Comp.OnXenoSlowResin = anySlowResin;
        ent.Comp.OnXenoFastResin = anyFastResin;
        Dirty(ent);
    }

    private void WeedsOvipositorChanged(Entity<AffectableByWeedsComponent> ent, ref XenoOvipositorChangedEvent args)
    {
        if (_affectedQuery.TryComp(ent, out var affected) && !affected.OnXenoSlowResin)
            _toUpdate.Add(ent);
    }

    public bool HasWeedsNearby(Entity<MapGridComponent> grid, EntityCoordinates coordinates, int range = 5)
    {
        var position = _mapSystem.LocalToTile(grid, grid, coordinates);
        var checkArea = new Box2(position.X - range + 1, position.Y - range + 1, position.X + range, position.Y + range);
        var enumerable = _mapSystem.GetLocalAnchoredEntities(grid, grid, checkArea);

        foreach (var anchored in enumerable)
        {
            if (TryComp<XenoWeedsComponent>(anchored, out var weeds) && weeds.IsSource)
                return true;
        }

        return false;
    }

    public bool IsOnHiveWeeds(Entity<MapGridComponent> grid, EntityCoordinates coordinates, bool sourceOnly = false)
    {
        var weed = GetWeedsOnFloor(grid, coordinates, sourceOnly);
        if (!TryComp(weed, out XenoWeedsComponent? weedComp))
        {
            return false;
        }

        // Some structures produce hive weed and act like a hive weed source, but they themselves are not hiveweeds.
        // For the purposes of this function, those structures are hive weed sources.
        return _prototype.TryIndex(weedComp.Spawns, out var spawns) &&
               spawns.HasComponent<HiveWeedsComponent>();
    }

    public bool IsOnWeeds(Entity<MapGridComponent> grid, EntityCoordinates coordinates, bool sourceOnly = false)
    {
        return GetWeedsOnFloor(grid, coordinates, sourceOnly) != null;
    }

    public Entity<XenoWeedsComponent>? GetWeedsOnFloor(Entity<MapGridComponent> grid, EntityCoordinates coordinates, bool sourceOnly = false)
    {
        var position = _mapSystem.LocalToTile(grid, grid, coordinates);
        var enumerator = _mapSystem.GetAnchoredEntitiesEnumerator(grid, grid, position);

        while (enumerator.MoveNext(out var anchored))
        {
            if (!_weedsQuery.TryComp(anchored, out var weeds))
                continue;

            if (!sourceOnly || weeds.IsSource)
                return (anchored.Value, weeds);
        }

        return null;
    }

    public EntityUid? GetWeedsOnFloor(EntityCoordinates coordinates, bool sourceOnly = false)
    {
        if (_transform.GetGrid(coordinates) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return null;

        return GetWeedsOnFloor((gridId, grid), coordinates, sourceOnly);
    }

    public bool IsOnWeeds(Entity<TransformComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        var coordinates = _transform.GetMoverCoordinates(entity, entity.Comp).SnapToGrid(EntityManager, _map);

        if (_transform.GetGrid(coordinates) is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? grid))
        {
            return false;
        }

        return IsOnWeeds((gridUid, grid), coordinates);
    }

    public bool IsOnFriendlyWeeds(Entity<TransformComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        var coordinates = _transform.GetMoverCoordinates(entity, entity.Comp).SnapToGrid(EntityManager, _map);

        if (_transform.GetGrid(coordinates) is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? grid))
        {
            return false;
        }

        var weeds = GetWeedsOnFloor((gridUid, grid), coordinates);
        if (weeds == null)
            return false;

        if (!_hive.FromSameHive(entity.Owner, weeds.Value.Owner))
            return false;

        return true;

    }

    private void OnResinSlowdownStartCollide(Entity<ResinSlowdownModifierComponent> ent, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (_affectedQuery.TryComp(other, out var affected) && !affected.OnXenoSlowResin)
            _toUpdate.Add(other);
    }

    private void OnResinSlowdownEndCollide(Entity<ResinSlowdownModifierComponent> ent, ref EndCollideEvent args)
    {
        var other = args.OtherEntity;
        if (_affectedQuery.TryComp(other, out var affected) && affected.OnXenoSlowResin)
            _toUpdate.Add(other);
    }

    private void OnResinSpeedupStartCollide(Entity<ResinSpeedupModifierComponent> ent, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (_affectedQuery.TryComp(other, out var affected) && !affected.OnXenoFastResin)
            _toUpdate.Add(other);
    }

    private void OnResinSpeedupEndCollide(Entity<ResinSpeedupModifierComponent> ent, ref EndCollideEvent args)
    {
        var other = args.OtherEntity;
        if (_affectedQuery.TryComp(other, out var affected) && affected.OnXenoFastResin)
            _toUpdate.Add(other);
    }

    private void OnSpreadingMapInit(Entity<XenoWeedsSpreadingComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.SpreadAt = _timing.CurTime + ent.Comp.SpreadDelay;
        Dirty(ent);
    }

    private void OnWeedOver(Entity<ReplaceWeedSourceOnWeedingComponent> weedSource, ref AfterEntityWeedingEvent args)
    {
        var (ent, comp) = weedSource;
        var weededEntity = _entities.GetEntity(args.CoveredEntity);

        if (!TryComp(ent, out XenoWeedsComponent? weedComp) ||
            Prototype(weededEntity) is not { } weededEntityProto ||
            !comp.ReplacementPairs.TryGetValue(weededEntityProto.ID, out var replacementId) ||
            TerminatingOrDeleted(weedSource) ||
            comp.HasReplaced)
        {
            return;
        }

        var newWeedSource = SpawnAtPosition(replacementId, weedSource.Owner.ToCoordinates());
        if (!TryComp(newWeedSource, out XenoWeedsComponent? newWeedSourceComp))
        {
            QueueDel(newWeedSource);
            return;
        }

        _hive.SetSameHive(ent, newWeedSource);

        var curWeeds = weedComp.Spread;
        foreach (var curWeed in curWeeds)
        {
            var curWeedComp = EnsureComp<XenoWeedsComponent>(curWeed);
            curWeedComp.Range = newWeedSourceComp.Range;
            curWeedComp.Source = newWeedSource;
            newWeedSourceComp.Spread.Add(curWeed);
        }
        curWeeds.Clear();
        RemComp<XenoWeedsSpreadingComponent>(newWeedSource);
        comp.HasReplaced = true;
        QueueDel(ent);
    }

    public bool CanSpreadWeedsPopup(Entity<MapGridComponent> grid, Vector2i tile, EntityUid? user, bool semiWeedable = false, bool source = false)
    {
        void GenericPopup()
        {
            if (user == null)
                return;

            var msg = Loc.GetString("cm-xeno-construction-failed-weeds");
            _popup.PopupClient(msg, user.Value, user.Value, PopupType.SmallCaution);
        }

        if (!_mapSystem.TryGetTileRef(grid, grid, tile, out var tileRef) ||
            !_tile.TryGetDefinition(tileRef.Tile.TypeId, out var tileDef) ||
            tileDef.ID == ContentTileDefinition.SpaceID ||
            (tileDef is ContentTileDefinition { WeedsSpreadable: false } &&
            !(tileDef is ContentTileDefinition { SemiWeedable: true } && semiWeedable))
            )
        {
            GenericPopup();
            return false;
        }

        if (!_area.CanResinPopup((grid, grid, null), tile, user))
            return false;

        var targetTileAnchored = _mapSystem.GetAnchoredEntitiesEnumerator(grid, grid, tile);
        while (targetTileAnchored.MoveNext(out var uid))
        {
            if (_blockWeedsQuery.HasComp(uid))
                return false;

            if (source && HasComp<XenoResinHoleComponent>(uid))
                return false;
        }

        return true;
    }

    public override void Update(float frameTime)
    {
        try
        {
            foreach (var mobId in _toUpdate)
            {
                UpdateQueued(mobId);
            }
        }
        finally
        {
            _toUpdate.Clear();
        }

        // Damage for not being over weeds
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<DamageOffWeedsComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var damage, out var damageable))
        {
            if ((TryComp(uid, out AffectableByWeedsComponent? affected) && affected.OnXenoWeeds) ||
                HasComp<InXenoTunnelComponent>(uid))
            {
                if (damage.DamageAt != null)
                {
                    damage.DamageAt = null;
                    Dirty(uid, damage);
                }

                continue;
            }
            else if (damage.DamageAt == null)
            {
                damage.DamageAt = time + damage.Every;
                Dirty(uid, damage);
            }

            if (time < damage.DamageAt)
                continue;

            damage.DamageAt = time + damage.Every;

            if (_container.TryGetContainingContainer((uid, null), out var container) &&
                _xenoQuery.HasComp(container.Owner))
            {
                continue;
            }

            if (!damage.RestingStopsDamage ||
                !HasComp<XenoRestingComponent>(uid))
            {
                _damageable.TryChangeDamage(uid, damage.Damage, damageable: damageable);
            }
        }
    }

    public bool CanPlaceWeedsPopup(EntityUid xeno,
        Entity<MapGridComponent> grid,
        EntityCoordinates coordinates,
        bool limitDistance)
    {
        if (_rmcMap.HasAnchoredEntityEnumerator<XenoWeedsComponent>(coordinates, out var oldWeeds))
        {
            if (oldWeeds.Comp.IsSource)
            {
                _popup.PopupClient("There's a pod here already!", oldWeeds, xeno, PopupType.SmallCaution);
                return false;
            }

            if (oldWeeds.Comp.BlockOtherWeeds)
            {
                _popup.PopupClient("These weeds are too strong to plant a node on!",
                    oldWeeds,
                    xeno,
                    PopupType.SmallCaution);
                return false;
            }
        }

        if (limitDistance && !HasWeedsNearby(grid, coordinates))
        {
            _popup.PopupClient("We can only plant weed nodes near other weed nodes our hive owns!",
                xeno,
                xeno,
                PopupType.SmallCaution);
            return false;
        }

        var entities = _mapSystem.GetAnchoredEntities(grid, coordinates.ToVector2i(EntityManager, _map, _transform));
        {
            foreach (var entity in entities)
            {
                if (!HasComp<ClimbableComponent>(entity) && !HasComp<RMCReactorPoweredLightComponent>(entity) ||
                    HasComp<BarricadeComponent>(entity))
                    continue;

                _popup.PopupClient(Loc.GetString("rmc-xeno-weeds-blocked"), xeno, xeno, PopupType.SmallCaution);
                return false;
            }
        }

        return true;
    }

    public void UpdateQueued(EntityUid update)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(update);
    }
}
