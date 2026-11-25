using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.ARES.Logs;
using Content.Shared._RMC14.ARES.Tabs;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.CrashLand;
using Content.Shared._RMC14.Extensions;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.ParaDrop;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Rules;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.SupplyDrop;

public abstract class SharedSupplyDropSystem : EntitySystem
{
    private const float LandingDamageBoundsPaddingFactor = 0.33f;

    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly ARESCoreSystem _core = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedCrashLandSystem _crashLand = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedParaDropSystem _paradrop = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly RMCCameraShakeSystem _rmcCameraShake = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCPullingSystem _rmcpulling = default!;

    private int _supplyDropCount;
    private MapId? _supplyDropMap;

    private readonly HashSet<Entity<CanBeSupplyDroppedComponent>> _crates = new();
    private readonly HashSet<EntityUid> _intersecting = new();

    private static readonly EntProtoId<ARESLogTypeComponent> LogCat = "ARESTabRequisitionsLogs";

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<BeingSupplyDroppedComponent, StorageOpenAttemptEvent>(OnBeingSupplyDroppedOpenAttempt);
        SubscribeLocalEvent<BeingSupplyDroppedComponent, ParaDropFinishedEvent>(OnBeingSupplyDroppedLanded);
        SubscribeLocalEvent<BeingSupplyDroppedComponent, CrashLandedEvent>(OnBeingSupplyDroppedLanded);
        SubscribeLocalEvent<BeingSupplyDroppedComponent, ComponentRemove>(OnBeingSupplyDroppedRemoved);

        Subs.BuiEvents<SupplyDropComputerComponent>(SupplyDropComputerUi.Key,
            subs =>
            {
                subs.Event<SupplyDropComputerLongitudeBuiMsg>(OnSupplyDropComputerLongitudeMsg);
                subs.Event<SupplyDropComputerLatitudeBuiMsg>(OnSupplyDropComputerLatitudeMsg);
                subs.Event<SupplyDropComputerUpdateBuiMsg>(OnSupplyDropComputerUpdateMsg);
                subs.Event<SupplyDropComputerLaunchBuiMsg>(OnSupplyDropComputerLaunchMsg);
            });
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _supplyDropCount = 0;
        _supplyDropMap = null;
    }

    private void OnBeingSupplyDroppedOpenAttempt(Entity<BeingSupplyDroppedComponent> ent, ref StorageOpenAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnBeingSupplyDroppedLanded<T>(Entity<BeingSupplyDroppedComponent> ent, ref T args)
    {
        if (_net.IsClient)
            return;

        RemoveWarningMarker(ent);

        if (ent.Comp.LandingDamage is { } landingDamage)
        {
            _intersecting.Clear();
            var bounds = _entityLookup.GetWorldAABB(ent);
            if (_transform.GetGrid(Transform(ent).Coordinates) is { } grid &&
                TryComp(grid, out MapGridComponent? gridComponent))
            {
                var center = _map.LocalToTile(grid, gridComponent, Transform(ent).Coordinates);
                var footprint = _crashLand.GetLandingFootprint(ent, (grid, gridComponent), center);
                if (footprint.Count > 0)
                {
                    var minimum = new Vector2(float.MaxValue);
                    var maximum = new Vector2(float.MinValue);
                    foreach (var tile in footprint)
                    {
                        var position = _map.GridTileToWorldPos(grid, gridComponent, tile.GridIndices);
                        minimum = Vector2.Min(minimum, position);
                        maximum = Vector2.Max(maximum, position);
                    }

                    bounds = new Box2(minimum, maximum)
                        .Enlarged(gridComponent.TileSize * LandingDamageBoundsPaddingFactor);
                }
            }

            _entityLookup.GetEntitiesIntersecting(Transform(ent).MapID, bounds, _intersecting);
            foreach (var intersecting in _intersecting)
            {
                if (!TryComp(intersecting, out TransformComponent? xform))
                    continue;

                if (intersecting == ent.Owner ||
                    _transform.IsParentOf(xform, ent.Owner) ||
                    _transform.ContainsEntity(ent.Owner, (intersecting, xform)))
                {
                    continue;
                }

                if (_container.TryGetContainingContainer((intersecting, xform, null), out var container) && (container.Owner == ent.Owner || HasComp<ParaDroppingComponent>(container.Owner) || HasComp<CrashLandingComponent>(container.Owner)))
                        continue;

                if (_container.TryGetOuterContainer(intersecting, xform, out var outer) && (outer.Owner == ent.Owner || HasComp<ParaDroppingComponent>(outer.Owner) || HasComp<CrashLandingComponent>(outer.Owner)))
                        continue;

                _damageable.TryChangeDamage(intersecting, landingDamage, true);
            }
        }

        var mapPos = _transform.GetMapCoordinates(ent);
        var filter = Filter.Empty().AddInRange(mapPos, 7);
        foreach (var recipient in filter.Recipients)
        {
            if (recipient.AttachedEntity is not { } player)
                continue;

            _rmcCameraShake.ShakeCamera(player, 4, 5);
        }

        ent.Comp.Landed = true;
        ent.Comp.OpenAt = _timing.CurTime + ent.Comp.OpenDelay;
        Dirty(ent);
    }

    private void OnBeingSupplyDroppedRemoved(Entity<BeingSupplyDroppedComponent> ent, ref ComponentRemove args)
    {
        RemoveWarningMarker(ent);
    }

    private void OnSupplyDropComputerLongitudeMsg(Entity<SupplyDropComputerComponent> ent, ref SupplyDropComputerLongitudeBuiMsg args)
    {
        SetLongitude((ent, ent), args.Longitude);
    }

    private void OnSupplyDropComputerLatitudeMsg(Entity<SupplyDropComputerComponent> ent, ref SupplyDropComputerLatitudeBuiMsg args)
    {
        SetLatitude((ent, ent), args.Latitude);
    }

    private void OnSupplyDropComputerUpdateMsg(Entity<SupplyDropComputerComponent> ent, ref SupplyDropComputerUpdateBuiMsg args)
    {
        if (_net.IsClient)
            return;

        UpdateHasCrate(ent);
    }

    private void OnSupplyDropComputerLaunchMsg(Entity<SupplyDropComputerComponent> ent, ref SupplyDropComputerLaunchBuiMsg args)
    {
        if (_net.IsClient)
            return;

        TryLaunchSupplyDropPopup(ent, args.Actor);
    }

    private bool TryGetPad(EntProtoId<SquadTeamComponent> squad, out Entity<SupplyDropPadComponent> pad)
    {
        var pads = EntityQueryEnumerator<SupplyDropPadComponent>();
        while (pads.MoveNext(out var uid, out var comp))
        {
            if (comp.Squad == squad)
            {
                pad = (uid, comp);
                return true;
            }
        }

        pad = default;
        return false;
    }

    public void SetSquad(Entity<SupplyDropComputerComponent?> computer, EntProtoId<SquadTeamComponent>? squad)
    {
        if (!Resolve(computer, ref computer.Comp, false))
            return;

        computer.Comp.Squad = squad;
        Dirty(computer);
    }

    public void SetLongitude(Entity<SupplyDropComputerComponent?> computer, int longitude)
    {
        if (!Resolve(computer, ref computer.Comp, false))
            return;

        longitude.Cap(computer.Comp.MaxCoordinate);
        computer.Comp.Coordinates = new Vector2i(longitude, computer.Comp.Coordinates.Y);
        Dirty(computer);
    }

    public void SetLatitude(Entity<SupplyDropComputerComponent?> computer, int latitude)
    {
        if (!Resolve(computer, ref computer.Comp, false))
            return;

        latitude.Cap(computer.Comp.MaxCoordinate);
        computer.Comp.Coordinates = new Vector2i(computer.Comp.Coordinates.X, latitude);
        Dirty(computer);
    }

    public bool TryFindCrate(Entity<SupplyDropComputerComponent> computer, out Entity<CanBeSupplyDroppedComponent> crate)
    {
        crate = default;
        if (computer.Comp.Squad is not { } squad ||
            !TryGetPad(squad, out var pad))
        {
            return false;
        }

        _crates.Clear();
        _entityLookup.GetEntitiesInRange(pad.Owner.ToCoordinates(), 0.25f, _crates);
        if (_crates.Count == 0)
            return false;

        crate = _crates.First();
        return true;
    }

    public bool TryLaunchSupplyDropPopup(Entity<SupplyDropComputerComponent> computer, EntityUid user)
    {
        var time = _timing.CurTime;
        if (time < computer.Comp.NextLaunchAt)
            return false;

        if (computer.Comp.Squad is not { } squad ||
            !_rmcPlanet.TryPlanetToCoordinates(computer.Comp.Coordinates, out var mapCoordinates) ||
            !CanSupplyDropSquad(squad))
        {
            _popup.PopupCursor(Loc.GetString("rmc-supply-drop-not-operational"), user, PopupType.MediumCaution);
            return false;
        }

        if (!TryFindCrate(computer, out var crate))
        {
            _popup.PopupCursor(Loc.GetString("rmc-supply-drop-no-crate"), user, PopupType.MediumCaution);
            return false;
        }

        SharedEntityStorageComponent? storage = null;
        if (_entityStorage.ResolveStorage(crate, ref storage) &&
            storage.Open)
        {
            _popup.PopupCursor(Loc.GetString("rmc-supply-drop-crate-open"), user, PopupType.MediumCaution);
            return false;
        }

        if (!_area.CanSupplyDrop(mapCoordinates))
        {
            _popup.PopupCursor(Loc.GetString("rmc-supply-drop-underground"), user, PopupType.MediumCaution);
            return false;
        }

        if (_rmcMap.IsTileBlocked(mapCoordinates) ||
            _rmcMap.TryGetTileDef(mapCoordinates, out var tile) && tile.ID == ContentTileDefinition.SpaceID)
        {
            _popup.PopupCursor(Loc.GetString("rmc-supply-drop-blocked"), user, PopupType.MediumCaution);
            return false;
        }

        var skyFallDuration = (float) crate.Comp.ArrivingSoundDelay.TotalSeconds;
        var dropDuration = (float) crate.Comp.DropDuration.TotalSeconds;
        var dropCoordinates = mapCoordinates.Offset(new Vector2(0.5f, 0.5f));
        var crateCoordinates = _transform.GetMoverCoordinates(crate);
        LaunchSupplyDrop(crate,
            dropCoordinates,
            skyFallDuration,
            dropDuration,
            crate.Comp.OpenDelay,
            crate.Comp.LandingDamage,
            crate.Comp.LandingEffectId,
            crate.Comp.ArrivingSound);

        _popup.PopupClient(Loc.GetString("rmc-supply-drop-crate-load", ("crate", crate)), crateCoordinates, user, PopupType.Medium);
        _marineAnnounce.AnnounceSquad(Loc.GetString("rmc-supply-drop-squad-announcement", ("crate", crate)), squad);
        _audio.PlayPvs(crate.Comp.LaunchSound, crateCoordinates);

        computer.Comp.LastLaunchAt = time;
        computer.Comp.NextLaunchAt = time + computer.Comp.Cooldown;
        Dirty(computer);
        _core.CreateARESLog(computer.Owner, LogCat, (string)$"{Name(user)} Launched a crate {Name(crate)} to {mapCoordinates.X}, {mapCoordinates.Y}.");
        return true;
    }

    public void LaunchSupplyDrop(EntityUid droppingEntity, MapCoordinates dropCoordinates, float skyFallDuration, float dropDuration, TimeSpan openDelay, DamageSpecifier? landingDamage = null, EntProtoId? landingEffect = null, SoundSpecifier? arrivingSound = null, int dropScatter = 0, bool useParachute = true, MapCoordinates? stagingCoordinates = null, bool playOpenSound = true)
    {
        if (_net.IsClient)
            return;

        _rmcpulling.TryStopAllPullsFromAndOn(droppingEntity);

        if (stagingCoordinates is { } staging)
            _transform.SetMapCoordinates(droppingEntity, staging);
        else
            _transform.SetMapCoordinates(droppingEntity, new MapCoordinates(_supplyDropCount++ * 50, 0, EnsureMap()));

        var dropping = EnsureComp<BeingSupplyDroppedComponent>(droppingEntity);
        var dropEntityCoordinates = _map.AlignToGrid(_transform.ToCoordinates(dropCoordinates));
        var validCoordinates = _paradrop.TryGetParaDropLocation(droppingEntity, dropEntityCoordinates, dropScatter, out var adjustedCoordinates);
        if (useParachute && validCoordinates)
        {
            dropEntityCoordinates = adjustedCoordinates;
        }

        dropping.Landed = false;
        dropping.OpenAt = TimeSpan.Zero;
        dropping.OpenDelay = openDelay;
        RemoveWarningMarker((droppingEntity, dropping));
        if (landingEffect is { } effect)
        {
            if (_transform.GetGrid(dropEntityCoordinates) is { } grid &&
                TryComp(grid, out MapGridComponent? gridComponent))
            {
                var center = _map.LocalToTile(grid, gridComponent, dropEntityCoordinates);
                var footprint = _crashLand.GetLandingFootprint(droppingEntity, (grid, gridComponent), center);

                foreach (var tile in footprint)
                {
                    dropping.LandingEffects.Add(Spawn(effect, _map.GridTileToLocal(grid, gridComponent, tile.GridIndices)));
                }
            }

            if (dropping.LandingEffects.Count == 0)
                dropping.LandingEffects.Add(Spawn(effect, dropEntityCoordinates));
        }

        dropping.LandingDamage = landingDamage;
        if (!playOpenSound)
            dropping.OpenSound = null;

        Dirty(droppingEntity, dropping);

        if (useParachute)
            _paradrop.DoParaDrop(droppingEntity, dropEntityCoordinates, skyFallDuration, dropDuration, arrivingSound);
        else
            _crashLand.DoCrashLand(droppingEntity, dropEntityCoordinates, skyFallDuration, dropDuration, false, arrivingSound);
    }

    private MapId EnsureMap()
    {
        if (!_map.MapExists(_supplyDropMap))
            _supplyDropMap = null;

        if (_supplyDropMap == null)
        {
            _map.CreateMap(out var map);
            _supplyDropMap = map;
        }

        return _supplyDropMap.Value;
    }

    private void UpdateHasCrate(Entity<SupplyDropComputerComponent> ent)
    {
        var hasCrate = ent.Comp.HasCrate;
        ent.Comp.HasCrate = TryFindCrate(ent, out _);
        if (hasCrate == ent.Comp.HasCrate)
            return;

        Dirty(ent);
    }

    private bool CanSupplyDropSquad(EntProtoId<SquadTeamComponent> squad)
    {
        if (!squad.TryGet(out var comp, _prototypes, _compFactory))
            return true;

        return comp.CanSupplyDrop;
    }

    private void RemoveWarningMarker(Entity<BeingSupplyDroppedComponent> ent)
    {
        foreach (var effect in ent.Comp.LandingEffects)
        {
            if (!TerminatingOrDeleted(effect))
                QueueDel(effect);
        }

        ent.Comp.LandingEffects.Clear();
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var computerQuery = EntityQueryEnumerator<SupplyDropComputerComponent>();
        while (computerQuery.MoveNext(out var uid, out var computer))
        {
            if (time < computer.NextUpdate)
                continue;

            computer.NextUpdate = time + computer.UpdateEvery;
            UpdateHasCrate((uid, computer));
        }

        var droppingQuery = EntityQueryEnumerator<BeingSupplyDroppedComponent>();
        while (droppingQuery.MoveNext(out var uid, out var dropping))
        {
            if (!dropping.Landed || time < dropping.OpenAt)
                continue;

            RemCompDeferred<BeingSupplyDroppedComponent>(uid);
            _audio.PlayPvs(dropping.OpenSound, _transform.GetMoverCoordinates(uid));
        }
    }
}
