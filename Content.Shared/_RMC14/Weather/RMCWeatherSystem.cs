using System.Numerics;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Campfire;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Light;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Decals;
using Content.Shared.Ghost;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weather;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weather;

public sealed class RMCWeatherSystem : EntitySystem
{
    private static readonly TimeSpan WeatherEffectInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan WeatherCleanInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan WeatherEffectMessageMinDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan WeatherEffectMessageMaxDelay = TimeSpan.FromSeconds(120);
    private const float CleanDecalChance = 0.05f;
    private const float WeatherBlockerLookupRadius = 0.05f;
    private const float WeatherSirenPopupRange = 7f;

    private static readonly SoundSpecifier MarineWeatherWarningSound =
        new SoundPathSpecifier("/Audio/_RMC14/Effects/radiostatic.ogg", AudioParams.Default.WithVolume(-4));

    private static readonly SoundSpecifier XenoWeatherWarningSound =
        new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_distantroar_3.ogg", AudioParams.Default.WithVolume(-4));

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedRoofSystem _roof = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly RMCAmbientLightSystem _rmcLight = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _flammable = default!;
    [Dependency] private readonly SharedXenoAcidSystem _acid = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDecalSystem _decals = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private EntityQuery<BlockWeatherComponent> _blockQuery;
    private readonly HashSet<Entity<RMCBlockWeatherComponent>> _weatherBlockers = new();
    private readonly List<(EntityUid GridUid, uint DecalId)> _decalsToRemove = new();

    public override void Initialize()
    {
        base.Initialize();
        _blockQuery = GetEntityQuery<BlockWeatherComponent>();

        SubscribeLocalEvent<RMCWeatherCycleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DropshipLandedOnPlanetEvent>(OnDropshipLandedOnPlanet);
    }

    private void OnMapInit(Entity<RMCWeatherCycleComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.WeatherEvents.Count <= 0)
            return;

        EnsureComp<RMCAmbientLightComponent>(ent);
        EnsureComp<RMCAmbientLightEffectsComponent>(ent);

        ent.Comp.State = RMCWeatherCycleState.Idle;
        ent.Comp.CurrentScreenOverlay = RMCWeatherScreenOverlay.None;
        ent.Comp.CurrentEventIndex = null;
        ent.Comp.ForcedEvent = null;
        ent.Comp.AdminForcedEvent = false;
        ent.Comp.CheckCooldown = ent.Comp.MinTimeBetweenEvents > TimeSpan.Zero
            ? _random.Next(ent.Comp.MinTimeBetweenEvents)
            : TimeSpan.Zero;
        ent.Comp.WarningRemaining = TimeSpan.Zero;
        ent.Comp.EventRemaining = TimeSpan.Zero;
        ent.Comp.LightningCooldown = TimeSpan.Zero;
        ent.Comp.EffectCooldown = WeatherEffectInterval;
        ent.Comp.CleanCooldown = WeatherCleanInterval;
        ent.Comp.FirstDropComplete = false;
        ent.Comp.EventStartedAt = TimeSpan.Zero;
        Dirty(ent);
    }

    private void OnDropshipLandedOnPlanet(ref DropshipLandedOnPlanetEvent ev)
    {
        if (_net.IsClient)
            return;

        var mapId = Transform(ev.Dropship.Owner).MapID;
        var weatherQuery = EntityQueryEnumerator<RMCWeatherCycleComponent>();
        while (weatherQuery.MoveNext(out var uid, out var cycle))
        {
            if (Transform(uid).MapID != mapId || cycle.FirstDropComplete)
                continue;

            cycle.FirstDropComplete = true;
            Dirty(uid, cycle);
        }
    }

    public bool TryStartWeatherEvent(MapId mapId, string eventKey, bool skipWarning, bool adminForced, out string message)
    {
        if (!TryGetWeatherCycle(mapId, out var ent))
        {
            message = Loc.GetString("rmc-weather-command-no-cycle", ("map", mapId));
            return false;
        }

        if (ent.Comp.State is RMCWeatherCycleState.Warning or RMCWeatherCycleState.Running)
        {
            message = Loc.GetString("rmc-weather-command-already-active",
                ("state", ent.Comp.State),
                ("map", mapId));
            return false;
        }

        if (!TryGetWeatherEvent(ent.Comp, eventKey, out var weatherEvent, out message))
            return false;

        StartWarning(ent, weatherEvent, skipWarning, adminForced);
        message = Loc.GetString("rmc-weather-command-started",
            ("weather", GetEventDisplayName(weatherEvent)),
            ("map", mapId));
        return true;
    }

    public bool TryEndWeather(MapId mapId, out string message)
    {
        if (!TryGetWeatherCycle(mapId, out var ent))
        {
            message = Loc.GetString("rmc-weather-command-no-cycle", ("map", mapId));
            return false;
        }

        if (ent.Comp.State is not (RMCWeatherCycleState.Warning or RMCWeatherCycleState.Running))
        {
            message = Loc.GetString("rmc-weather-command-no-active", ("map", mapId));
            return false;
        }

        EndWeather(ent);
        message = Loc.GetString("rmc-weather-command-ended", ("map", mapId));
        return true;
    }

    public string GetWeatherStatus(MapId mapId)
    {
        if (!TryGetWeatherCycle(mapId, out var ent))
            return Loc.GetString("rmc-weather-command-no-cycle", ("map", mapId));

        var status = Loc.GetString("rmc-weather-command-status",
            ("map", mapId),
            ("state", ent.Comp.State));
        if (TryGetCurrentEvent(ent.Comp, out var weatherEvent))
        {
            status = Loc.GetString("rmc-weather-command-status-event",
                ("status", status),
                ("weather", GetEventDisplayName(weatherEvent)),
                ("seconds", (int) Math.Max(ent.Comp.EventRemaining.TotalSeconds, 0)));
        }

        return Loc.GetString(ent.Comp.FirstDropComplete
                ? "rmc-weather-command-status-first-drop-complete"
                : "rmc-weather-command-status-waiting-first-drop",
            ("status", status));
    }

    public IEnumerable<string> GetWeatherEventOptions(MapId mapId)
    {
        if (!TryGetWeatherCycle(mapId, out var ent))
            yield break;

        for (var i = 0; i < ent.Comp.WeatherEvents.Count; i++)
        {
            var weatherEvent = ent.Comp.WeatherEvents[i];
            yield return i.ToString();
            yield return weatherEvent.Name;

            if (weatherEvent.DisplayName != null)
            {
                var localizedName = GetLocalizedDisplayName(weatherEvent);
                if (localizedName != null)
                    yield return localizedName;
            }
        }
    }

    public bool CanWeatherAffectArea(EntityUid uid, MapGridComponent grid, TileRef tileRef, RoofComponent? roofComp = null)
    {
        if (Resolve(uid, ref roofComp, false) && _roof.IsRooved((uid, grid, roofComp), tileRef.GridIndices))
            return false;

        if (!_area.IsWeatherEnabled((uid, grid), tileRef.GridIndices))
            return false;

        var anchoredEntities = _mapSystem.GetAnchoredEntitiesEnumerator(uid, grid, tileRef.GridIndices);

        while (anchoredEntities.MoveNext(out var ent))
        {
            if (_blockQuery.HasComponent(ent.Value))
                return false;
        }

        return true;
    }

    public bool TryGetCurrentScreenOverlay(MapId mapId, out RMCWeatherScreenOverlay overlay)
    {
        overlay = RMCWeatherScreenOverlay.None;

        var weatherQuery = EntityQueryEnumerator<RMCWeatherCycleComponent>();
        while (weatherQuery.MoveNext(out var uid, out var cycle))
        {
            if (Transform(uid).MapID != mapId ||
                cycle.State != RMCWeatherCycleState.Running ||
                cycle.CurrentScreenOverlay == RMCWeatherScreenOverlay.None ||
                (byte) cycle.CurrentScreenOverlay <= (byte) overlay)
            {
                continue;
            }

            overlay = cycle.CurrentScreenOverlay;
        }

        return overlay != RMCWeatherScreenOverlay.None;
    }

    public bool TryGetCurrentExamineRange(MapId mapId, out float range)
    {
        range = float.MaxValue;
        var found = false;

        var weatherQuery = EntityQueryEnumerator<RMCWeatherCycleComponent>();
        while (weatherQuery.MoveNext(out var uid, out var cycle))
        {
            if (Transform(uid).MapID != mapId ||
                cycle.State != RMCWeatherCycleState.Running ||
                cycle.CurrentScreenOverlay == RMCWeatherScreenOverlay.None)
            {
                continue;
            }

            range = Math.Min(range, RMCWeatherScreenOverlayData.GetClearRange(cycle.CurrentScreenOverlay));
            found = true;
        }

        return found;
    }

    public bool IsWeatherExposed(EntityUid uid)
    {
        if (!TryComp(uid, out TransformComponent? xform))
            return false;

        return IsWeatherExposed(uid, xform.MapID);
    }

    public void HandleWeatherEffects(Entity<RMCWeatherCycleComponent> ent, RMCWeatherEvent weatherEvent)
    {
        if (weatherEvent.LightningChance <= 0 || weatherEvent.LightningEffects.Count <= 0)
            return;

        if (!_random.Prob(weatherEvent.LightningChance))
            return;

        EnsureComp<RMCAmbientLightComponent>(ent, out var lightComp);
        EnsureComp<MapLightComponent>(ent, out var mapLightComp);

        // The lightning color sequences animate black to black, so skip if something else is already driving light.
        if (lightComp.IsAnimating || mapLightComp.AmbientLightColor != Color.Black)
            return;

        var lightningEffect = _rmcLight.ProcessPrototype(_random.Pick(weatherEvent.LightningEffects));
        _rmcLight.SetColor((ent, lightComp), lightningEffect, weatherEvent.LightningDuration);

        _audio.PlayGlobal(_audio.ResolveSound(weatherEvent.LightningSound), Filter.BroadcastMap(Transform(ent).MapID), true);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var delta = TimeSpan.FromSeconds(frameTime);
        var weatherQuery = EntityQueryEnumerator<RMCWeatherCycleComponent>();

        while (weatherQuery.MoveNext(out var uid, out var cycle))
        {
            if (cycle.WeatherEvents.Count <= 0)
                continue;

            switch (cycle.State)
            {
                case RMCWeatherCycleState.Idle:
                    UpdateIdle((uid, cycle), delta);
                    break;
                case RMCWeatherCycleState.Warning:
                    UpdateWarning((uid, cycle), delta);
                    break;
                case RMCWeatherCycleState.Running:
                    UpdateRunning((uid, cycle), delta);
                    break;
                case RMCWeatherCycleState.Cooldown:
                    UpdateCooldown((uid, cycle), delta);
                    break;
            }
        }
    }

    private void UpdateIdle(Entity<RMCWeatherCycleComponent> ent, TimeSpan delta)
    {
        ent.Comp.CheckCooldown -= delta;
        if (ent.Comp.CheckCooldown > TimeSpan.Zero)
            return;

        if (!_random.Prob(Math.Clamp(ent.Comp.StartChance, 0, 1)))
        {
            ent.Comp.CheckCooldown = GetCheckDelay(ent.Comp);
            Dirty(ent);
            return;
        }

        StartWarning(ent, _random.Next(ent.Comp.WeatherEvents.Count));
    }

    private void UpdateWarning(Entity<RMCWeatherCycleComponent> ent, TimeSpan delta)
    {
        ent.Comp.WarningRemaining -= delta;
        if (ent.Comp.WarningRemaining > TimeSpan.Zero)
            return;

        StartWeather(ent);
    }

    private void UpdateRunning(Entity<RMCWeatherCycleComponent> ent, TimeSpan delta)
    {
        if (!TryGetCurrentEvent(ent.Comp, out var weatherEvent))
        {
            EndWeather(ent);
            return;
        }

        ent.Comp.EventRemaining -= delta;
        if (ent.Comp.EventRemaining <= TimeSpan.Zero)
        {
            EndWeather(ent);
            return;
        }

        ent.Comp.LightningCooldown -= delta;
        if (ent.Comp.LightningCooldown <= TimeSpan.Zero)
        {
            HandleWeatherEffects(ent, weatherEvent);
            ent.Comp.LightningCooldown = weatherEvent.LightningCooldownDuration;
        }

        ent.Comp.EffectCooldown -= delta;
        if (ent.Comp.EffectCooldown <= TimeSpan.Zero)
        {
            ProcessGameplayEffects(ent, weatherEvent);
            ent.Comp.EffectCooldown = WeatherEffectInterval;
        }

        if (!weatherEvent.CleansDecals || !ent.Comp.FirstDropComplete)
            return;

        ent.Comp.CleanCooldown -= delta;
        if (ent.Comp.CleanCooldown > TimeSpan.Zero)
            return;

        ProcessWeatherCleaning(ent, weatherEvent);
        ent.Comp.CleanCooldown = WeatherCleanInterval;
    }

    private void UpdateCooldown(Entity<RMCWeatherCycleComponent> ent, TimeSpan delta)
    {
        ent.Comp.CheckCooldown -= delta;
        if (ent.Comp.CheckCooldown > TimeSpan.Zero)
            return;

        ent.Comp.State = RMCWeatherCycleState.Idle;
        ent.Comp.CheckCooldown = TimeSpan.Zero;
        Dirty(ent);
    }

    private void StartWarning(Entity<RMCWeatherCycleComponent> ent, int eventIndex)
    {
        ent.Comp.CurrentEventIndex = eventIndex;
        ent.Comp.ForcedEvent = null;
        ent.Comp.AdminForcedEvent = false;
        ent.Comp.WarningRemaining = ent.Comp.WarnTime;
        ent.Comp.State = RMCWeatherCycleState.Warning;
        SendWeatherWarning(ent, ent.Comp.WeatherEvents[eventIndex]);
        Dirty(ent);

        if (ent.Comp.WarningRemaining <= TimeSpan.Zero)
            StartWeather(ent);
    }

    private void StartWarning(Entity<RMCWeatherCycleComponent> ent, RMCWeatherEvent weatherEvent, bool skipWarning, bool adminForced)
    {
        ent.Comp.CurrentEventIndex = null;
        ent.Comp.ForcedEvent = weatherEvent;
        ent.Comp.AdminForcedEvent = adminForced;
        ent.Comp.WarningRemaining = skipWarning ? TimeSpan.Zero : ent.Comp.WarnTime;
        ent.Comp.State = RMCWeatherCycleState.Warning;
        SendWeatherWarning(ent, weatherEvent);
        Dirty(ent);

        if (ent.Comp.WarningRemaining <= TimeSpan.Zero)
            StartWeather(ent);
    }

    private void StartWeather(Entity<RMCWeatherCycleComponent> ent)
    {
        if (!TryGetCurrentEvent(ent.Comp, out var weatherEvent))
        {
            ent.Comp.State = RMCWeatherCycleState.Idle;
            ent.Comp.CurrentScreenOverlay = RMCWeatherScreenOverlay.None;
            ent.Comp.CheckCooldown = GetCheckDelay(ent.Comp);
            Dirty(ent);
            return;
        }

        BeginWeather(ent, weatherEvent, ent.Comp.AdminForcedEvent);
    }

    private void BeginWeather(Entity<RMCWeatherCycleComponent> ent, RMCWeatherEvent weatherEvent, bool adminForced)
    {
        if (!_proto.TryIndex(weatherEvent.WeatherType, out var weatherProto))
        {
            Log.Error($"Unable to find RMC weather prototype {weatherEvent.WeatherType} for {ToPrettyString(ent)}.");
            ent.Comp.State = RMCWeatherCycleState.Idle;
            ent.Comp.CurrentScreenOverlay = RMCWeatherScreenOverlay.None;
            ent.Comp.CheckCooldown = GetCheckDelay(ent.Comp);
            Dirty(ent);
            return;
        }

        var mapId = Transform(ent).MapID;
        var endTime = _timing.CurTime + weatherEvent.Duration;
        ent.Comp.State = RMCWeatherCycleState.Running;
        ent.Comp.CurrentScreenOverlay = weatherEvent.ScreenOverlay;
        ent.Comp.EventRemaining = weatherEvent.Duration;
        ent.Comp.LightningCooldown = TimeSpan.Zero;
        ent.Comp.EffectCooldown = WeatherEffectInterval;
        ent.Comp.CleanCooldown = WeatherCleanInterval;
        ent.Comp.EventStartedAt = _timing.CurTime;
        ent.Comp.AdminForcedEvent = adminForced;
        ent.Comp.EventSequence++;
        _weather.SetWeather(mapId, weatherProto, endTime);
        Dirty(ent);

        var ev = new RMCWeatherStartedEvent(mapId, GetEventDisplayName(weatherEvent), weatherEvent.Duration, adminForced);
        RaiseLocalEvent(ref ev);
    }

    private void EndWeather(Entity<RMCWeatherCycleComponent> ent)
    {
        var mapId = Transform(ent).MapID;
        var hasEvent = TryGetCurrentEvent(ent.Comp, out var weatherEvent);
        var elapsed = hasEvent && ent.Comp.EventStartedAt > TimeSpan.Zero
            ? _timing.CurTime - ent.Comp.EventStartedAt
            : (TimeSpan?) null;
        var adminForced = ent.Comp.AdminForcedEvent;

        _weather.SetWeather(mapId, null, null);
        ent.Comp.State = RMCWeatherCycleState.Cooldown;
        ent.Comp.CurrentScreenOverlay = RMCWeatherScreenOverlay.None;
        ent.Comp.CurrentEventIndex = null;
        ent.Comp.ForcedEvent = null;
        ent.Comp.AdminForcedEvent = false;
        ent.Comp.CheckCooldown = ent.Comp.MinTimeBetweenEvents;
        ent.Comp.WarningRemaining = TimeSpan.Zero;
        ent.Comp.EventRemaining = TimeSpan.Zero;
        ent.Comp.LightningCooldown = TimeSpan.Zero;
        ent.Comp.EffectCooldown = WeatherEffectInterval;
        ent.Comp.CleanCooldown = WeatherCleanInterval;
        ent.Comp.EventStartedAt = TimeSpan.Zero;
        Dirty(ent);

        if (hasEvent)
        {
            var ev = new RMCWeatherEndedEvent(mapId, GetEventDisplayName(weatherEvent), elapsed, adminForced);
            RaiseLocalEvent(ref ev);
        }
    }

    private bool TryGetWeatherCycle(MapId mapId, out Entity<RMCWeatherCycleComponent> cycle)
    {
        var weatherQuery = EntityQueryEnumerator<RMCWeatherCycleComponent>();
        while (weatherQuery.MoveNext(out var uid, out var comp))
        {
            if (Transform(uid).MapID != mapId)
                continue;

            cycle = (uid, comp);
            return true;
        }

        cycle = default;
        return false;
    }

    private bool TryGetWeatherEvent(RMCWeatherCycleComponent cycle, string eventKey, out RMCWeatherEvent weatherEvent, out string message)
    {
        weatherEvent = default!;
        if (int.TryParse(eventKey, out var index))
        {
            if (index >= 0 && index < cycle.WeatherEvents.Count)
            {
                weatherEvent = cycle.WeatherEvents[index];
                message = string.Empty;
                return true;
            }

            message = Loc.GetString("rmc-weather-command-index-out-of-range", ("index", index));
            return false;
        }

        foreach (var candidate in cycle.WeatherEvents)
        {
            if (string.Equals(candidate.Name, eventKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.DisplayName, eventKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(GetLocalizedDisplayName(candidate), eventKey, StringComparison.OrdinalIgnoreCase))
            {
                weatherEvent = candidate;
                message = string.Empty;
                return true;
            }
        }

        message = Loc.GetString("rmc-weather-command-unknown-event", ("event", eventKey));
        return false;
    }

    private string GetEventDisplayName(RMCWeatherEvent weatherEvent)
    {
        return GetLocalizedDisplayName(weatherEvent) ?? weatherEvent.Name;
    }

    private string? GetLocalizedDisplayName(RMCWeatherEvent weatherEvent)
    {
        if (weatherEvent.DisplayName == null)
            return null;

        return Loc.TryGetString(weatherEvent.DisplayName, out var localized)
            ? localized
            : weatherEvent.DisplayName;
    }

    private bool TryGetCurrentEvent(RMCWeatherCycleComponent cycle, out RMCWeatherEvent weatherEvent)
    {
        weatherEvent = default!;
        if (cycle.ForcedEvent != null)
        {
            weatherEvent = cycle.ForcedEvent;
            return true;
        }

        if (cycle.CurrentEventIndex is not { } index ||
            index < 0 ||
            index >= cycle.WeatherEvents.Count)
        {
            return false;
        }

        weatherEvent = cycle.WeatherEvents[index];
        return true;
    }

    private TimeSpan GetCheckDelay(RMCWeatherCycleComponent cycle)
    {
        var delay = cycle.MinTimeBetweenChecks;
        if (cycle.MinCheckVariance > TimeSpan.Zero)
            delay += _random.Next(cycle.MinCheckVariance) - TimeSpan.FromTicks(cycle.MinCheckVariance.Ticks / 2);

        return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
    }

    private void SendWeatherWarning(Entity<RMCWeatherCycleComponent> ent, RMCWeatherEvent weatherEvent)
    {
        if (weatherEvent.WarningMode == RMCWeatherWarningMode.None)
            return;

        var mapId = Transform(ent).MapID;
        if (weatherEvent.WarningMode == RMCWeatherWarningMode.SirenOnly)
        {
            if (TryPlayPhysicalWeatherSirens(mapId, weatherEvent))
                return;

            if (weatherEvent.WarningSound is { } fallbackSound)
            {
                Log.Warning($"No RMC weather sirens of kind {weatherEvent.WarningSirenKind?.ToString() ?? "<unset>"} found on map {mapId} for {weatherEvent.Name}; playing global fallback warning sound.");
                _audio.PlayGlobal(_audio.ResolveSound(fallbackSound), Filter.BroadcastMap(mapId), true);
            }

            return;
        }

        if (weatherEvent.WarningSound is { } warningSound)
            _audio.PlayGlobal(_audio.ResolveSound(warningSound), Filter.BroadcastMap(mapId), true);

        var displayName = GetEventDisplayName(weatherEvent);
        var marineMessage = Loc.GetString("rmc-weather-warning-marine", ("weather", displayName));
        var marineFilter = Filter.BroadcastMap(mapId).AddWhereAttachedEntity(e =>
            HasComp<MarineComponent>(e) ||
            HasComp<GhostComponent>(e));

        _marineAnnounce.AnnounceToMarines(marineMessage, MarineWeatherWarningSound, marineFilter);

        var xenoMessage = Loc.GetString("rmc-weather-warning-xeno", ("weather", displayName));
        var xenoFilter = Filter.BroadcastMap(mapId).AddWhereAttachedEntity(HasComp<XenoComponent>);
        _xenoAnnounce.Announce(default,
            xenoFilter,
            xenoMessage,
            _xenoAnnounce.WrapHive(xenoMessage),
            XenoWeatherWarningSound);
    }

    private bool TryPlayPhysicalWeatherSirens(MapId mapId, RMCWeatherEvent weatherEvent)
    {
        if (weatherEvent.WarningSirenKind is not { } sirenKind)
            return false;

        var played = false;
        var popupRecipients = new Dictionary<ICommonSession, (EntityUid Recipient, string Message, float DistanceSquared)>();
        var query = EntityQueryEnumerator<RMCWeatherSirenComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var siren, out var xform))
        {
            if (xform.MapID != mapId || siren.Kind != sirenKind)
                continue;

            _audio.PlayPvs(siren.WarningSound, uid);
            AddWeatherSirenPopupRecipients(uid, siren, xform, popupRecipients);
            played = true;
        }

        foreach (var (session, popup) in popupRecipients)
        {
            _popup.PopupEntity(popup.Message,
                popup.Recipient,
                session,
                PopupType.LargeCaution);
        }

        return played;
    }

    private void AddWeatherSirenPopupRecipients(
        EntityUid sirenUid,
        RMCWeatherSirenComponent siren,
        TransformComponent sirenXform,
        Dictionary<ICommonSession, (EntityUid Recipient, string Message, float DistanceSquared)> popupRecipients)
    {
        var sirenCoords = _transform.GetMapCoordinates(sirenXform);
        var sirenPos = sirenCoords.Position;
        var filter = Filter.Empty().AddInRange(sirenCoords, WeatherSirenPopupRange, entMan: EntityManager);
        foreach (var session in filter.Recipients)
        {
            if (session.AttachedEntity is not { } recipient ||
                !TryComp(recipient, out TransformComponent? recipientXform) ||
                recipientXform.MapID != sirenXform.MapID)
            {
                continue;
            }

            var distanceSquared = (_transform.GetWorldPosition(recipientXform) - sirenPos).LengthSquared();
            if (popupRecipients.TryGetValue(session, out var existing) &&
                existing.DistanceSquared <= distanceSquared)
            {
                continue;
            }

            popupRecipients[session] = (
                recipient,
                Loc.GetString(siren.WarningMessage, ("siren", sirenUid)),
                distanceSquared);
        }
    }

    private void ProcessGameplayEffects(Entity<RMCWeatherCycleComponent> ent, RMCWeatherEvent weatherEvent)
    {
        var mapId = Transform(ent).MapID;

        if (weatherEvent.FireSmotheringStrength > 0)
        {
            ProcessBurningEntities(mapId, weatherEvent.FireSmotheringStrength);
            ProcessTileFires(mapId, weatherEvent.FireSmotheringStrength);
            ProcessCampfires(mapId, weatherEvent.FireSmotheringStrength);
            ProcessAcidDilution(mapId, weatherEvent.FireSmotheringStrength, ent.Comp.EventSequence);
        }

        if (!weatherEvent.ExposureDamage.Empty || weatherEvent.EffectMessage != null)
            ProcessMobEffects(mapId, weatherEvent);
    }

    private void ProcessBurningEntities(MapId mapId, int fireSmotheringStrength)
    {
        var query = EntityQueryEnumerator<FlammableComponent, OnFireComponent>();
        while (query.MoveNext(out var uid, out var flammable, out _))
        {
            if (!flammable.OnFire || Transform(uid).MapID != mapId || !IsWeatherExposed(uid, mapId))
                continue;

            _flammable.AdjustStacks((uid, flammable), -fireSmotheringStrength);
        }
    }

    private void ProcessTileFires(MapId mapId, int fireSmotheringStrength)
    {
        var reduction = TimeSpan.FromSeconds(fireSmotheringStrength);
        var query = EntityQueryEnumerator<TileFireComponent>();
        while (query.MoveNext(out var uid, out var fire))
        {
            if (Transform(uid).MapID != mapId || !IsWeatherExposed(uid, mapId))
                continue;

            var remaining = fire.SpawnedAt + fire.Duration - _timing.CurTime;
            if (remaining <= reduction)
            {
                QueueDel(uid);
                continue;
            }

            fire.Duration -= reduction;
            Dirty(uid, fire);
        }
    }

    private void ProcessCampfires(MapId mapId, int fireSmotheringStrength)
    {
        var reduction = TimeSpan.FromSeconds(fireSmotheringStrength);
        var query = EntityQueryEnumerator<CampfireComponent>();
        while (query.MoveNext(out var uid, out var campfire))
        {
            if (!campfire.Lit || Transform(uid).MapID != mapId || !IsWeatherExposed(uid, mapId))
                continue;

            var ev = new CampfireWeatherSmotherEvent(reduction);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void ProcessAcidDilution(MapId mapId, int fireSmotheringStrength, int eventSequence)
    {
        var multiplier = Math.Clamp(1 - fireSmotheringStrength * 0.1f, 0, 1);
        if (multiplier >= 1)
            return;

        var timedQuery = EntityQueryEnumerator<TimedCorrodingComponent>();
        while (timedQuery.MoveNext(out var uid, out var timed))
        {
            if (!TryMarkAcidDiluted(uid, mapId, eventSequence))
                continue;

            if (multiplier <= 0)
            {
                _acid.RemoveAcid(uid);
                continue;
            }

            timed.CorrodesAt = _timing.CurTime + (timed.CorrodesAt - _timing.CurTime) * multiplier;
            timed.Dps *= multiplier;
            timed.LightDps *= multiplier;
            Dirty(uid, timed);
        }

        var damageableQuery = EntityQueryEnumerator<DamageableCorrodingComponent>();
        while (damageableQuery.MoveNext(out var uid, out var damageable))
        {
            if (!TryMarkAcidDiluted(uid, mapId, eventSequence))
                continue;

            if (multiplier <= 0)
            {
                _acid.RemoveAcid(uid);
                continue;
            }

            damageable.AcidExpiresAt = _timing.CurTime + (damageable.AcidExpiresAt - _timing.CurTime) * multiplier;
            damageable.Dps *= multiplier;
            damageable.Damage = damageable.Damage * multiplier;
            Dirty(uid, damageable);
        }
    }

    private bool TryMarkAcidDiluted(EntityUid uid, MapId mapId, int eventSequence)
    {
        if (TryComp(uid, out RMCWeatherDilutedAcidComponent? weatherAcid) &&
            weatherAcid.LastWeatherSequence == eventSequence)
        {
            return false;
        }

        if (Transform(uid).MapID != mapId)
            return false;

        if (!IsWeatherExposed(uid, mapId))
            return false;

        weatherAcid ??= EnsureComp<RMCWeatherDilutedAcidComponent>(uid);

        weatherAcid.LastWeatherSequence = eventSequence;
        Dirty(uid, weatherAcid);
        return true;
    }

    private void ProcessMobEffects(MapId mapId, RMCWeatherEvent weatherEvent)
    {
        var query = EntityQueryEnumerator<MobStateComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var mobState, out var damageable))
        {
            if (!_mobState.IsAlive(uid, mobState) ||
                Transform(uid).MapID != mapId ||
                !IsWeatherExposed(uid, mapId))
            {
                continue;
            }

            if (!weatherEvent.ExposureDamage.Empty)
            {
                var damage = HasComp<XenoComponent>(uid)
                    ? weatherEvent.ExposureDamage * 3
                    : weatherEvent.ExposureDamage;

                _damageable.TryChangeDamage(uid, damage, true, false, damageable);
            }

            if (weatherEvent.EffectMessage is { } effectMessage &&
                CanShowWeatherEffectMessage(uid, weatherEvent))
            {
                _popup.PopupEntity(Loc.GetString(effectMessage), uid, uid, PopupType.SmallCaution);
            }
        }
    }

    private bool CanShowWeatherEffectMessage(EntityUid uid, RMCWeatherEvent weatherEvent)
    {
        if (weatherEvent.EffectMessage == null)
            return false;

        if (TryComp(uid, out RMCWeatherEffectPopupCooldownComponent? cooldown) &&
            cooldown.NextPopupAt > _timing.CurTime)
        {
            return false;
        }

        if (!_random.Prob(Math.Clamp(weatherEvent.EffectMessageChance, 0, 1)))
            return false;

        cooldown ??= EnsureComp<RMCWeatherEffectPopupCooldownComponent>(uid);
        cooldown.NextPopupAt = _timing.CurTime + _random.Next(WeatherEffectMessageMinDelay, WeatherEffectMessageMaxDelay);
        return true;
    }

    private void ProcessWeatherCleaning(Entity<RMCWeatherCycleComponent> ent, RMCWeatherEvent weatherEvent)
    {
        var mapId = Transform(ent).MapID;
        var grids = EntityQueryEnumerator<MapGridComponent, DecalGridComponent>();
        while (grids.MoveNext(out var gridUid, out var mapGrid, out var decalGrid))
        {
            if (Transform(gridUid).MapID != mapId)
                continue;

            _decalsToRemove.Clear();
            foreach (var chunk in decalGrid.ChunkCollection.ChunkCollection.Values)
            {
                foreach (var (decalId, decal) in chunk.Decals)
                {
                    if (!decal.Cleanable)
                        continue;

                    var coords = new EntityCoordinates(gridUid, decal.Coordinates);
                    if (!_mapSystem.TryGetTileRef(gridUid, mapGrid, coords, out var tile) ||
                        !_weather.CanWeatherAffect(gridUid, mapGrid, tile) ||
                        IsRMCWeatherBlocked(mapId, _transform.ToMapCoordinates(coords).Position))
                    {
                        continue;
                    }

                    if (_random.Prob(CleanDecalChance))
                        _decalsToRemove.Add((gridUid, decalId));
                }
            }

            foreach (var (removeGrid, decalId) in _decalsToRemove)
            {
                _decals.RemoveDecal(removeGrid, decalId);
            }
        }
    }

    private bool IsWeatherExposed(EntityUid uid, MapId mapId)
    {
        if (!TryGetWeatherTile(uid, mapId, out var grid, out var tile))
            return false;

        if (IsRMCWeatherBlocked(mapId, _transform.GetMapCoordinates(uid).Position))
            return false;

        return _weather.CanWeatherAffect(grid.Owner, grid.Comp, tile);
    }

    private bool IsRMCWeatherBlocked(MapId mapId, Vector2 position)
    {
        _weatherBlockers.Clear();
        var bounds = new Box2(
            position - new Vector2(WeatherBlockerLookupRadius, WeatherBlockerLookupRadius),
            position + new Vector2(WeatherBlockerLookupRadius, WeatherBlockerLookupRadius));
        _lookup.GetEntitiesIntersecting(mapId, bounds, _weatherBlockers, LookupFlags.Uncontained);

        foreach (var blocker in _weatherBlockers)
        {
            var uid = blocker.Owner;
            if (!TryComp(uid, out TransformComponent? xform))
                continue;

            var blockerBounds = _lookup.GetAABBNoContainer(uid,
                _transform.GetWorldPosition(xform),
                _transform.GetWorldRotation(xform));

            if (blockerBounds.Contains(position))
                return true;
        }

        return false;
    }

    private bool TryGetWeatherTile(EntityUid uid, MapId mapId, out Entity<MapGridComponent> grid, out TileRef tile)
    {
        grid = default;
        tile = default;

        var coords = _transform.GetMoverCoordinates(uid);
        if (_transform.GetGrid(coords) is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? gridComp) ||
            Transform(gridUid).MapID != mapId ||
            !_mapSystem.TryGetTileRef(gridUid, gridComp, coords, out tile))
        {
            return false;
        }

        grid = (gridUid, gridComp);
        return true;
    }
}
