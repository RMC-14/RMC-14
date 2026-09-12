using Content.Server.Destructible;
using Content.Server.Medical.SuitSensors;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Medical.CrewMonitoring;
using Content.Shared._RMC14.Roles;
using Content.Shared._RMC14.Rules;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Medical.CrewMonitoring;

public sealed class RMCCrewMonitorSystem : SharedRMCCrewMonitorSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly RMCCrewMonitorDataSystem _data = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

    private readonly Dictionary<EntityUid, Entity<SuitSensorComponent>> _sensors = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCCrewMonitorComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<RMCCrewMonitorComponent, BreakageEventArgs>(OnBreakage);
        SubscribeLocalEvent<RMCCrewMonitorComponent, DamageChangedEvent>(OnDamageChanged, after: [typeof(DestructibleSystem)]);
        SubscribeLocalEvent<RMCCrewMonitorComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<RMCCrewMonitorComponent, RMCCrewMonitorRefreshBuiMsg>(OnRefresh);
    }

    private void OnBreakage(Entity<RMCCrewMonitorComponent> ent, ref BreakageEventArgs args)
    {
        _appearance.SetData(ent, RMCCrewMonitorVisuals.Broken, true);
        _ui.CloseUis(ent.Owner);
    }

    private void OnDamageChanged(Entity<RMCCrewMonitorComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageIncreased ||
            !TryComp(ent, out DestructibleComponent? destructible) ||
            destructible.IsBroken)
        {
            return;
        }

        _appearance.SetData(ent, RMCCrewMonitorVisuals.Broken, false);
    }

    private void OnUIOpenAttempt(Entity<RMCCrewMonitorComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (TryComp(ent, out DestructibleComponent? destructible) && destructible.IsBroken)
            args.Cancel();
    }

    private void OnUIOpened(Entity<RMCCrewMonitorComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey.Equals(RMCCrewMonitorUIKey.Key))
            RefreshConsole(ent);
    }

    private void OnRefresh(Entity<RMCCrewMonitorComponent> ent, ref RMCCrewMonitorRefreshBuiMsg args)
    {
        RefreshConsole(ent);
    }

    private void RefreshConsole(Entity<RMCCrewMonitorComponent> ent)
    {
        ent.Comp.Entries = BuildSnapshot(ent.Comp);
        Dirty(ent);
    }

    private List<RMCCrewMonitorEntry> BuildSnapshot(RMCCrewMonitorComponent monitor)
    {
        _data.CollectSensors(
            monitor.NpcFactions,
            monitor.IffFactions,
            SuitSensorMode.SensorBinary,
            _sensors);

        var entries = new List<RMCCrewMonitorEntry>(_sensors.Count);
        foreach (var (user, sensor) in _sensors)
        {
            if (TryCreateEntry(user, sensor.Comp, out var entry))
                entries.Add(entry);
        }

        return entries;
    }

    private bool TryCreateEntry(EntityUid user, SuitSensorComponent sensor, out RMCCrewMonitorEntry entry)
    {
        entry = default;
        if (!TryComp(user, out MobStateComponent? mobState) ||
            !TryComp(user, out TransformComponent? xform) ||
            !_map.TryGetMap(xform.MapID, out var mapId))
        {
            return false;
        }

        var isPlanet = HasComp<RMCPlanetComponent>(mapId);
        var isShip = HasComp<AlmayerComponent>(mapId);
        if (!isPlanet && !isShip)
            return false;

        var identity = _data.GetIdentity(user);

        string? squad = null;
        Color? squadColor = null;
        var squadName = new GetMarineSquadNameEvent(string.Empty, string.Empty);
        RaiseLocalEvent(user, ref squadName);
        if (!string.IsNullOrWhiteSpace(squadName.SquadName))
            squad = squadName.SquadName;

        var squadIcon = new GetMarineIconEvent(null, null, null);
        RaiseLocalEvent(user, ref squadIcon);
        squadColor = squadIcon.BackgroundColor;

        var state = mobState.CurrentState;
        if (sensor.Mode == SuitSensorMode.SensorBinary && state != MobState.Dead)
            state = MobState.Alive;

        float? brute = null;
        float? burn = null;
        float? toxin = null;
        float? oxygen = null;
        if (sensor.Mode >= SuitSensorMode.SensorVitals && TryComp(user, out DamageableComponent? damageable))
        {
            brute = damageable.DamagePerGroup.GetValueOrDefault(BruteGroup).Float();
            burn = damageable.DamagePerGroup.GetValueOrDefault(BurnGroup).Float();
            toxin = damageable.DamagePerGroup.GetValueOrDefault(ToxinGroup).Float();
            oxygen = damageable.DamagePerGroup.GetValueOrDefault(AirlossGroup).Float();
        }

        RMCCrewMonitorLocation? location = null;
        string? areaName = null;
        if (sensor.Mode == SuitSensorMode.SensorCords)
        {
            location = isPlanet
                ? RMCCrewMonitorLocation.Planet
                : RMCCrewMonitorLocation.Ship;

            var coordinates = _transform.GetMapCoordinates(user);
            if (_area.TryGetArea(coordinates, out _, out var areaPrototype))
                areaName = areaPrototype.Name;
        }

        entry = new RMCCrewMonitorEntry(
            GetNetEntity(user),
            identity.Name,
            identity.JobTitle,
            identity.Job,
            identity.JobIcon,
            identity.Departments,
            squad,
            squadColor,
            sensor.Mode,
            state,
            brute,
            burn,
            toxin,
            oxygen,
            location,
            areaName);
        return true;
    }

}
