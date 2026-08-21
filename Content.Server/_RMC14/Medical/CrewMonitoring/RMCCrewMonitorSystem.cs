using Content.Server.Access.Systems;
using Content.Server.Destructible;
using Content.Server.Medical.SuitSensors;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Medical.CrewMonitoring;
using Content.Shared._RMC14.Roles;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Access.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Medical.CrewMonitoring;

public sealed class RMCCrewMonitorSystem : SharedRMCCrewMonitorSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

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
        var unique = new Dictionary<EntityUid, Entity<SuitSensorComponent>>();
        var sensors = EntityQueryEnumerator<SuitSensorComponent>();
        while (sensors.MoveNext(out var sensorId, out var sensor))
        {
            if (sensor.Mode == SuitSensorMode.SensorOff ||
                sensor.User is not { } user ||
                TerminatingOrDeleted(user) ||
                (!HasComp<ActorComponent>(user) && !HasComp<OriginalRoleComponent>(user)) ||
                !TryComp(user, out TransformComponent? xform) ||
                !_map.TryGetMap(xform.MapID, out var mapId) ||
                _map.IsPaused(mapId.Value) ||
                !IsTracked(user, monitor))
            {
                continue;
            }

            if (unique.TryGetValue(user, out var existing) && existing.Comp.Mode >= sensor.Mode)
                continue;

            unique[user] = (sensorId, sensor);
        }

        var entries = new List<RMCCrewMonitorEntry>(unique.Count);
        foreach (var (user, sensor) in unique)
        {
            if (TryCreateEntry(user, sensor.Comp, out var entry))
                entries.Add(entry);
        }

        return entries;
    }

    private bool IsTracked(EntityUid user, RMCCrewMonitorComponent monitor)
    {
        if (_npcFaction.IsMemberOfAny((user, null), monitor.NpcFactions))
            return true;

        foreach (var faction in monitor.IffFactions)
        {
            if (_gunIFF.IsInFaction(user, faction))
                return true;
        }

        return false;
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

        var name = Loc.GetString("suit-sensor-component-unknown-name");
        var jobTitle = Loc.GetString("suit-sensor-component-unknown-job");
        ProtoId<JobIconPrototype> jobIcon = "JobIconNoId";
        var departments = new List<ProtoId<DepartmentPrototype>>();
        if (_idCard.TryFindIdCard(user, out var card))
        {
            if (!string.IsNullOrWhiteSpace(card.Comp.FullName))
                name = card.Comp.FullName;
            if (!string.IsNullOrWhiteSpace(card.Comp.LocalizedJobTitle))
                jobTitle = card.Comp.LocalizedJobTitle;
            jobIcon = card.Comp.JobIcon;
            departments.AddRange(card.Comp.JobDepartments);
        }

        ProtoId<JobPrototype>? job = null;
        if (TryComp(user, out OriginalRoleComponent? originalRole))
            job = originalRole.Job;

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
            name,
            jobTitle,
            job,
            jobIcon,
            departments,
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
