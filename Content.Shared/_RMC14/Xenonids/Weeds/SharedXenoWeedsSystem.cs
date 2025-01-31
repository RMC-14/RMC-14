using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.Construction.FloorResin;
using Content.Shared._RMC14.Xenonids.Construction.ResinHole;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Maps;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Weeds;

public abstract class SharedXenoWeedsSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    private readonly HashSet<EntityUid> _toUpdate = new();

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

        SubscribeLocalEvent<XenoWeedableComponent, AnchorStateChangedEvent>(OnWeedableAnchorStateChanged);

        SubscribeLocalEvent<DamageOffWeedsComponent, MapInitEvent>(OnDamageOffWeedsMapInit);

        SubscribeLocalEvent<AffectableByWeedsComponent, RefreshMovementSpeedModifiersEvent>(WeedsRefreshPassiveSpeed);

        SubscribeLocalEvent<XenoWeedsComponent, StartCollideEvent>(OnWeedsStartCollide);
        SubscribeLocalEvent<XenoWeedsComponent, EndCollideEvent>(OnWeedsEndCollide);

        SubscribeLocalEvent<XenoWeedsSpreadingComponent, MapInitEvent>(OnSpreadingMapInit);

        SubscribeLocalEvent<ResinSlowdownModifierComponent, ComponentShutdown>(OnModifierShutdown);
        SubscribeLocalEvent<ResinSlowdownModifierComponent, StartCollideEvent>(OnResinSlowdownStartCollide);
        SubscribeLocalEvent<ResinSlowdownModifierComponent, EndCollideEvent>(OnResinSlowdownEndCollide);

        SubscribeLocalEvent<ResinSpeedupModifierComponent, ComponentShutdown>(OnModifierShutdown);
        SubscribeLocalEvent<ResinSpeedupModifierComponent, StartCollideEvent>(OnResinSpeedupStartCollide);
        SubscribeLocalEvent<ResinSpeedupModifierComponent, EndCollideEvent>(OnResinSpeedupEndCollide);

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
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

    private void OnWeedableAnchorStateChanged(Entity<XenoWeedableComponent> weedable, ref AnchorStateChangedEvent args)
    {
        if (_net.IsServer && !args.Anchored)
            QueueDel(weedable.Comp.Entity);
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
        var entriesResin = 0;
        var entriesWeeds = 0;
        foreach (var contacting in _physics.GetContactingEntities(ent, physicsComponent))
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
        {
            speedWeeds /= entriesWeeds;
        }

        if (entriesResin > 0)
        {
            speedResin /= entriesResin;
        }

        //If Weeds is a speedup, let them stack, otherwise treat them as slowdownss
        if ((speedWeeds > 1 || speedResin > 1) && entriesResin > 0 && entriesWeeds > 0)
            finalSpeed = speedWeeds * speedResin;
        else if (entriesResin > 0)
            finalSpeed = speedResin;
        else if (entriesWeeds > 0)
            finalSpeed = speedWeeds;

        args.ModifySpeed(finalSpeed, finalSpeed);

        ent.Comp.OnXenoWeeds = anyWeeds;
        ent.Comp.OnXenoSlowResin = anySlowResin;
        ent.Comp.OnXenoFastResin = anyFastResin;
        Dirty(ent);
    }

    public bool IsOnWeeds(Entity<MapGridComponent> grid, EntityCoordinates coordinates, bool sourceOnly = false)
    {
        return (GetWeedsOnFloor(grid, coordinates, sourceOnly) is EntityUid);
    }

    public EntityUid? GetWeedsOnFloor(Entity<MapGridComponent> grid, EntityCoordinates coordinates, bool sourceOnly = false)
    {
        var position = _mapSystem.LocalToTile(grid, grid, coordinates);
        var enumerator = _mapSystem.GetAnchoredEntitiesEnumerator(grid, grid, position);

        while (enumerator.MoveNext(out var anchored))
        {
            if (!_weedsQuery.TryComp(anchored, out var weeds))
                continue;

            if (!sourceOnly || weeds.IsSource)
                return anchored;
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

    public bool CanPlaceWeedsPopup(Entity<MapGridComponent> grid, Vector2i tile, EntityUid? user, bool semiWeedable = false, bool source = false)
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
                _movementSpeed.RefreshMovementSpeedModifiers(mobId);
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
}
