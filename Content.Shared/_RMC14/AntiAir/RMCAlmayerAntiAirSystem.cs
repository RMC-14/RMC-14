using System.Linq;
using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.ARES.Logs;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.AntiAir;

public sealed class RMCAlmayerAntiAirSystem : EntitySystem
{
    [Dependency] private readonly ARESCoreSystem _core = default!;
    [Dependency] private readonly RMCCameraShakeSystem _cameraShake = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private static readonly EntProtoId<ARESLogTypeComponent> LogCat = "ARESTabDropshipLogs";
    private readonly List<EntityUid> _destinations = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCAlmayerAntiAirComponent, BoundUIOpenedEvent>(OnBuiOpened);

        SubscribeLocalEvent<RMCGetHijackDestinationEvent>(OnGetHijackDestination);
        SubscribeLocalEvent<RMCDropshipHijackAntiAirResolvedEvent>(OnDropshipHijackAntiAirResolved);

        Subs.BuiEvents<RMCAlmayerAntiAirComponent>(RMCAlmayerAntiAirUiKey.Key, subs =>
        {
            subs.Event<RMCAlmayerAntiAirSetZoneBuiMsg>(OnSetZoneBui);
            subs.Event<RMCAlmayerAntiAirClearZoneBuiMsg>(OnClearZoneBui);
        });
    }

    private void OnBuiOpened(Entity<RMCAlmayerAntiAirComponent> ent, ref BoundUIOpenedEvent args)
    {
        RefreshConsoleUi(ent);
    }

    private void OnSetZoneBui(Entity<RMCAlmayerAntiAirComponent> ent, ref RMCAlmayerAntiAirSetZoneBuiMsg args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Disabled)
        {
            _popup.PopupClient(Loc.GetString("rmc-anti-air-console-disabled"), ent.Owner, args.Actor, PopupType.MediumCaution);
            return;
        }

        var key = GetDefenseKey(ent.Owner);
        if (!TryGetZone(key, args.Zone, out var zone))
        {
            Log.Warning($"{ToPrettyString(args.Actor)} tried to set MGAD to invalid defense zone {args.Zone}");
            return;
        }

        ent.Comp.ProtectedZone = zone.Zone;
        Dirty(ent);
        RefreshConsoleUi(ent);
        RefreshOverwatch();

        var message = Loc.GetString("rmc-anti-air-ares-target-set", ("zone", zone.Zone));
        _core.CreateARESLog(ent, LogCat, (string)$"{Name(args.Actor)} set the IX-50 MGAD to protect {zone.Zone}");
        _adminLog.Add(LogType.RMCMarineAnnounce, $"{ToPrettyString(args.Actor)} set IX-50 MGAD to protect {zone.Zone}");
        _popup.PopupClient(message, ent.Owner, args.Actor);
    }

    private void OnClearZoneBui(Entity<RMCAlmayerAntiAirComponent> ent, ref RMCAlmayerAntiAirClearZoneBuiMsg args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Disabled)
        {
            _popup.PopupClient(Loc.GetString("rmc-anti-air-console-disabled"), ent.Owner, args.Actor, PopupType.MediumCaution);
            return;
        }

        ent.Comp.ProtectedZone = null;
        Dirty(ent);
        RefreshConsoleUi(ent);
        RefreshOverwatch();

        _core.CreateARESLog(ent, LogCat, (string)$"{Name(args.Actor)} cleared IX-50 MGAD defense targeting");
        _adminLog.Add(LogType.RMCMarineAnnounce, $"{ToPrettyString(args.Actor)} cleared IX-50 MGAD defense targeting");
        _popup.PopupClient(Loc.GetString("rmc-anti-air-ares-target-cleared"), ent.Owner, args.Actor);
    }

    private void OnGetHijackDestination(ref RMCGetHijackDestinationEvent ev)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(ev.OriginalDestination, out DropshipHijackDestinationComponent? destination))
            return;

        var key = GetDefenseKey(ev.OriginalDestination);
        if (!TryGetDestinationZone(key, ev.OriginalDestination, destination, out var requestedZone))
            return;

        ev.OriginalZone = requestedZone.Zone;
        ev.Destination = PickDestinationInZone(key, requestedZone.Zone) ?? ev.OriginalDestination;

        if (!TryGetOperationalAntiAir(key, out var antiAir) ||
            antiAir is not { } antiAirEnt)
        {
            return;
        }

        ev.AntiAirConsole = antiAirEnt.Owner;
        ev.DeterrenceSound = antiAirEnt.Comp.DeterrenceSound;
        ev.DeterrenceShakeIntensity = antiAirEnt.Comp.DeterrenceShakeIntensity;
        ev.DeterrenceShakeDuration = antiAirEnt.Comp.DeterrenceShakeDuration;

        if (antiAirEnt.Comp.ProtectedZone != requestedZone.Zone)
            return;

        if (!TryPickAlternateDestination(key, requestedZone.Zone, out var divertedDestination, out var divertedZone))
        {
            Log.Warning($"IX-50 MGAD protected {requestedZone.Zone}, but no alternate hijack defense zone exists for {key}");
            return;
        }

        ev.Destination = divertedDestination;
        ev.DivertedZone = divertedZone.Zone;
        ev.Deterrence = true;
    }

    private void OnDropshipHijackAntiAirResolved(ref RMCDropshipHijackAntiAirResolvedEvent ev)
    {
        if (_net.IsClient)
            return;

        if (ev.AntiAirConsole is { } console &&
            TryComp(console, out RMCAlmayerAntiAirComponent? antiAir) &&
            antiAir.DisableOnHijack)
        {
            antiAir.Disabled = true;
            Dirty(console, antiAir);
            RefreshConsoleUi((console, antiAir));
            RefreshOverwatch();
        }

        if (!ev.Deterrence ||
            ev.OriginalZone == null ||
            ev.DivertedZone == null)
        {
            return;
        }

        var comp = EnsureComp<RMCAlmayerAntiAirDropshipComponent>(ev.Dropship);
        comp.OriginalZone = ev.OriginalZone;
        comp.DivertedZone = ev.DivertedZone;
        comp.Sound = ev.DeterrenceSound ?? comp.Sound;
        comp.ShakeIntensity = ev.DeterrenceShakeIntensity;
        comp.ShakeDuration = ev.DeterrenceShakeDuration;
        comp.Announced = false;

        if (TryComp(ev.Dropship, out DropshipComponent? dropship) && dropship.HijackLandAt is { } landAt)
            comp.AnnounceAt = _timing.CurTime + (landAt - _timing.CurTime) / 2;
        else
            comp.AnnounceAt = _timing.CurTime;

        _core.CreateARESLog(ev.Dropship, LogCat, (string)$"IX-50 MGAD deterred a hijacked dropship from {ev.OriginalZone} to {ev.DivertedZone}");
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCAlmayerAntiAirDropshipComponent, DropshipComponent>();
        while (query.MoveNext(out var uid, out var antiAir, out var dropship))
        {
            if (antiAir.Announced || time < antiAir.AnnounceAt)
                continue;

            antiAir.Announced = true;

            var message = Loc.GetString("rmc-anti-air-hijack-deterred", ("zone", antiAir.OriginalZone));
            _marineAnnounce.AnnounceARESStaging(uid, message, null, new LocId("rmc-announcement-dropship-message"));

            PlayDeterrenceEffects(uid, antiAir);
        }
    }

    private void PlayDeterrenceEffects(EntityUid dropship, RMCAlmayerAntiAirDropshipComponent antiAir)
    {
        if (!TryComp(dropship, out TransformComponent? xform) ||
            xform.MapID == MapId.Nullspace)
        {
            return;
        }

        var grid = xform.GridUid ?? dropship;
        var filter = Filter.BroadcastMap(xform.MapID)
            .RemoveWhereAttachedEntity(entity => _transform.GetGrid(entity) != grid);

        _audio.PlayGlobal(antiAir.Sound, filter, true);
        _cameraShake.ShakeCamera(filter, antiAir.ShakeIntensity, antiAir.ShakeDuration);
    }

    public List<(NetEntity Id, string Name)> GetHijackDestinations()
    {
        var all = new List<(NetEntity Id, string Name)>();
        var zoneDestinations = new Dictionary<(RMCShipDefenseGridKey Key, string Zone), (EntityUid Destination, RMCShipDefenseZoneEntry Zone)>();
        var hasMappedZone = false;
        var query = EntityQueryEnumerator<DropshipHijackDestinationComponent, MetaDataComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var destination, out var meta, out var xform))
        {
            all.Add((GetNetEntity(uid), meta.EntityName));
            hasMappedZone |= !string.IsNullOrWhiteSpace(destination.DefenseZone);

            var key = GetDefenseKey(xform);
            if (!TryGetDestinationZone(key, uid, destination, out var zone))
                continue;

            zoneDestinations.TryAdd((key, zone.Zone), (uid, zone));
        }

        if (!hasMappedZone || zoneDestinations.Count == 0)
            return all;

        return zoneDestinations.Values
            .Select(entry => (GetNetEntity(entry.Destination), entry.Zone.Zone))
            .ToList();
    }

    public RMCAlmayerAntiAirStatus GetStatus(EntityUid context)
    {
        var key = GetDefenseKey(context);
        var query = EntityQueryEnumerator<RMCAlmayerAntiAirComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var antiAir, out var xform))
        {
            if (GetDefenseKey(xform) != key)
                continue;

            return new RMCAlmayerAntiAirStatus(true, antiAir.Disabled, antiAir.ProtectedZone);
        }

        return new RMCAlmayerAntiAirStatus(false, false, null);
    }

    public void RefreshConsoleUi(Entity<RMCAlmayerAntiAirComponent> ent)
    {
        if (_net.IsClient || !_ui.IsUiOpen(ent.Owner, RMCAlmayerAntiAirUiKey.Key))
            return;

        var key = GetDefenseKey(ent.Owner);
        ent.Comp.Zones = GetZones(key);
        Dirty(ent);
    }

    private List<RMCShipDefenseZoneEntry> GetZones(RMCShipDefenseGridKey key)
    {
        var zones = GetMappedZones(key);
        return zones.Count == 0
            ? GetLegacyZones(key)
            : zones;
    }

    private List<RMCShipDefenseZoneEntry> GetMappedZones(RMCShipDefenseGridKey key)
    {
        var zones = new List<RMCShipDefenseZoneEntry>();
        var seen = new HashSet<string>();
        var query = EntityQueryEnumerator<DropshipHijackDestinationComponent, TransformComponent>();
        while (query.MoveNext(out _, out var destination, out var xform))
        {
            if (GetDefenseKey(xform) != key || string.IsNullOrWhiteSpace(destination.DefenseZone))
                continue;

            var defenseZone = destination.DefenseZone.Trim();
            if (!seen.Add(defenseZone))
                continue;

            zones.Add(new RMCShipDefenseZoneEntry(defenseZone));
        }

        return zones;
    }

    private List<RMCShipDefenseZoneEntry> GetLegacyZones(RMCShipDefenseGridKey key)
    {
        var zones = new List<RMCShipDefenseZoneEntry>();
        var query = EntityQueryEnumerator<DropshipHijackDestinationComponent, MetaDataComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var meta, out var xform))
        {
            if (GetDefenseKey(xform) != key)
                continue;

            zones.Add(new RMCShipDefenseZoneEntry(meta.EntityName));
        }

        return zones;
    }

    private bool TryGetZone(RMCShipDefenseGridKey key, string defenseZone, out RMCShipDefenseZoneEntry zone)
    {
        foreach (var entry in GetZones(key))
        {
            if (entry.Zone == defenseZone)
            {
                zone = entry;
                return true;
            }
        }

        zone = default;
        return false;
    }

    private bool TryGetDestinationZone(
        RMCShipDefenseGridKey key,
        EntityUid uid,
        DropshipHijackDestinationComponent destination,
        out RMCShipDefenseZoneEntry zone)
    {
        if (!string.IsNullOrWhiteSpace(destination.DefenseZone) &&
            TryGetZone(key, destination.DefenseZone.Trim(), out zone))
        {
            return true;
        }

        if (GetMappedZones(key).Count != 0)
        {
            zone = default;
            return false;
        }

        zone = new RMCShipDefenseZoneEntry(Name(uid));
        return true;
    }

    private bool TryGetOperationalAntiAir(RMCShipDefenseGridKey key, out Entity<RMCAlmayerAntiAirComponent>? antiAir)
    {
        var query = EntityQueryEnumerator<RMCAlmayerAntiAirComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (GetDefenseKey(xform) != key || comp.Disabled)
            {
                continue;
            }

            antiAir = (uid, comp);
            return true;
        }

        antiAir = null;
        return false;
    }

    private EntityUid? PickDestinationInZone(RMCShipDefenseGridKey key, string defenseZone)
    {
        _destinations.Clear();
        var query = EntityQueryEnumerator<DropshipHijackDestinationComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var destination, out var xform))
        {
            if (GetDefenseKey(xform) == key &&
                TryGetDestinationZone(key, uid, destination, out var zone) &&
                zone.Zone == defenseZone)
            {
                _destinations.Add(uid);
            }
        }

        return _destinations.Count == 0
            ? null
            : _random.Pick(_destinations);
    }

    private bool TryPickAlternateDestination(
        RMCShipDefenseGridKey key,
        string protectedZone,
        out EntityUid destination,
        out RMCShipDefenseZoneEntry zone)
    {
        var zones = new Dictionary<string, RMCShipDefenseZoneEntry>();
        foreach (var entry in GetZones(key))
        {
            if (entry.Zone != protectedZone)
                zones[entry.Zone] = entry;
        }

        var candidates = new Dictionary<string, List<EntityUid>>();
        var query = EntityQueryEnumerator<DropshipHijackDestinationComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var hijack, out var xform))
        {
            if (GetDefenseKey(xform) != key ||
                !TryGetDestinationZone(key, uid, hijack, out var destinationZone) ||
                !zones.ContainsKey(destinationZone.Zone))
            {
                continue;
            }

            candidates.GetOrNew(destinationZone.Zone).Add(uid);
        }

        if (candidates.Count == 0)
        {
            destination = default;
            zone = default;
            return false;
        }

        var defenseZone = _random.Pick(candidates.Keys.ToList());
        destination = _random.Pick(candidates[defenseZone]);
        zone = zones[defenseZone];
        return true;
    }

    private void RefreshOverwatch()
    {
        var ev = new RMCAlmayerAntiAirChangedEvent();
        RaiseLocalEvent(ref ev);
    }

    private RMCShipDefenseGridKey GetDefenseKey(EntityUid uid)
    {
        return GetDefenseKey(Transform(uid));
    }

    private static RMCShipDefenseGridKey GetDefenseKey(TransformComponent xform)
    {
        return new RMCShipDefenseGridKey(xform.MapID, xform.GridUid);
    }
}

[ByRefEvent]
public readonly record struct RMCAlmayerAntiAirChangedEvent;

internal readonly record struct RMCShipDefenseGridKey(MapId MapId, EntityUid? Grid);
