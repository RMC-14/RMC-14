using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Extensions;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Rules;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.SupplyDrop;

public abstract class SharedSupplyDropSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly INetManager _net = default!;
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

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<BeingSupplyDroppedComponent, StorageOpenAttemptEvent>(OnBeingSupplyDroppedOpenAttempt);

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

        SharedEntityStorageComponent? storage = null;
        if (_entityStorage.ResolveStorage(crate, ref storage) &&
            storage.Open)
        {
            _popup.PopupCursor(Loc.GetString("rmc-supply-drop-crate-open"), user, PopupType.MediumCaution);
            return false;
        }

        var crateCoordinates = _transform.GetMoverCoordinates(crate);
        _popup.PopupClient(Loc.GetString("rmc-supply-drop-crate-load", ("crate", crate)), crateCoordinates, user, PopupType.Medium);
        _audio.PlayPredicted(crate.Comp.LaunchSound, crateCoordinates, user);
        _marineAnnounce.AnnounceSquad(Loc.GetString("rmc-supply-drop-squad-announcement", ("crate", crate)), squad);
        _rmcpulling.TryStopAllPullsFromAndOn(crate);

        var mapId = EnsureMap();
        _transform.SetMapCoordinates(crate, new MapCoordinates(_supplyDropCount++ * 50, 0, mapId));

        var dropping = EnsureComp<BeingSupplyDroppedComponent>(crate);
        dropping.Target = _transform.ToCoordinates(mapCoordinates).Offset(new Vector2(0.5f, -0.5f));
        dropping.ArrivingSoundAt = time + crate.Comp.ArrivingSoundDelay;
        dropping.DropAt = time + crate.Comp.DropDelay;
        dropping.OpenAt = time + crate.Comp.OpenDelay;
        dropping.LandingEffect = Spawn(crate.Comp.LandingEffectId, dropping.Target);
        dropping.LandingDamage = crate.Comp.LandingDamage;
        Dirty(crate, dropping);

        computer.Comp.LastLaunchAt = time;
        computer.Comp.NextLaunchAt = time + computer.Comp.Cooldown;
        Dirty(computer);
        return true;
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
            if (!dropping.PlayedArrivingSound &&
                time > dropping.ArrivingSoundAt &&
                dropping.LandingEffect != null)
            {
                dropping.PlayedArrivingSound = true;
                _audio.PlayPvs(dropping.ArrivingSound, _transform.GetMoverCoordinates(dropping.LandingEffect.Value));
                Dirty(uid, dropping);
            }

            if (time < dropping.DropAt)
                continue;

            if (!dropping.Landed)
            {
                dropping.Landed = true;
                if (!TerminatingOrDeleted(dropping.LandingEffect))
                {
                    QueueDel(dropping.LandingEffect);
                    dropping.LandingEffect = null;
                    Dirty(uid, dropping);
                }

                if (dropping.LandingDamage is { } landingDamage)
                {
                    _intersecting.Clear();
                    _entityLookup.GetEntitiesInRange(dropping.Target, 0.33f, _intersecting);
                    foreach (var intersecting in _intersecting)
                    {
                        _damageable.TryChangeDamage(intersecting, landingDamage, true);
                    }
                }

                _transform.SetCoordinates(uid, _transform.GetMoverCoordinates(dropping.Target));
                var mapPos = _transform.ToMapCoordinates(dropping.Target);
                var filter = Filter.Empty().AddInRange(mapPos, 7);
                foreach (var recipient in filter.Recipients)
                {
                    if (recipient.AttachedEntity is not { } player)
                        continue;

                    _rmcCameraShake.ShakeCamera(player, 4, 5);
                }
            }

            if (time < dropping.OpenAt)
                continue;

            RemCompDeferred<BeingSupplyDroppedComponent>(uid);
            _audio.PlayPvs(dropping.OpenSound, uid);
        }
    }
}
