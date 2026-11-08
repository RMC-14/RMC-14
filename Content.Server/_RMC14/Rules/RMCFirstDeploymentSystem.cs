using System.Globalization;
using Content.Server._RMC14.Rules.DistressSignal;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Intel;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.RMCClock;
using Content.Shared._RMC14.Roles;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Survivor;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Rules;

public sealed class RMCFirstDeploymentSystem : EntitySystem
{
    [Dependency] private readonly RMCAnnouncementOverlaySystem _announcementOverlay = default!;
    [Dependency] private readonly CMDistressSignalRuleSystem _distressSignal = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly RMCClockSystem _rmcClock = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipLaunchedFromWarshipEvent>(OnDropshipLaunchedFromWarship);
    }

    private void OnDropshipLaunchedFromWarship(ref DropshipLaunchedFromWarshipEvent ev)
    {
        if (ev.Dropship.Comp.Destination is not { } destination ||
            !TryComp(destination, out TransformComponent? destinationTransform) ||
            !_rmcPlanet.IsOnPlanet(destinationTransform))
        {
            return;
        }

        var query = EntityQueryEnumerator<RMCFirstDeploymentComponent, ActiveGameRuleComponent>();
        while (query.MoveNext(out var deployment, out _))
        {
            if (deployment.Announced || deployment.AnnounceAt != null)
                continue;

            deployment.WarshipName = GetWarshipName(ev.Warship);
            deployment.AnnounceAt = _timing.CurTime + deployment.Delay;
        }
    }

    private void Announce(RMCFirstDeploymentComponent deployment)
    {
        var operation = _distressSignal.OperationName;
        var planet = _distressSignal.SelectedPlanetMapName;
        if (string.IsNullOrWhiteSpace(operation) || string.IsNullOrWhiteSpace(planet))
        {
            Log.Warning("Could not show the first deployment announcement because the operation or planet name was missing.");
            return;
        }

        var warship = deployment.WarshipName ?? GetAnyWarshipName();
        if (warship == null)
        {
            Log.Warning("Could not show the first deployment announcement because the warship name was missing.");
            return;
        }

        var title = FormatOperationName(operation);
        var time = FormatTime(_rmcClock.GetWorldTime());
        var recipients = new Dictionary<LocId, Filter>();
        foreach (var session in _player.Sessions)
        {
            if (session.AttachedEntity is not { } entity || !IsRecipient(entity))
                continue;

            var assignment = GetAssignment(CompOrNull<OriginalRoleComponent>(entity)?.Job) ?? deployment.DefaultAssignment;
            if (!recipients.TryGetValue(assignment, out var filter))
            {
                filter = Filter.Empty();
                recipients.Add(assignment, filter);
            }

            filter.AddPlayer(session);
        }

        foreach (var (assignment, filter) in recipients)
        {
            var text = Loc.GetString("rmc-announcement-first-deployment-message",
                ("time", time),
                ("planet", planet),
                ("assignment", Loc.GetString(assignment, ("warship", warship))));

            _announcementOverlay.Show(new RMCAnnouncementOverlay(deployment.Overlay, text, Title: title), filter);
        }
    }

    private bool IsRecipient(EntityUid entity)
    {
        return HasComp<MarineComponent>(entity) &&
               !HasComp<GhostComponent>(entity) &&
               !HasComp<RMCSurvivorComponent>(entity) &&
               !HasComp<IntelRescueSurvivorObjectiveComponent>(entity) &&
               TryComp(entity, out MobStateComponent? mobState) &&
               mobState.CurrentState != MobState.Dead &&
               !_rmcPlanet.IsOnPlanet(Transform(entity));
    }

    private LocId? GetAssignment(ProtoId<JobPrototype>? job)
    {
        if (job is not { } jobId || !_prototypes.TryIndex(jobId, out var jobPrototype))
            return null;

        if (jobPrototype.FirstDeploymentAssignment is { } jobAssignment)
            return jobAssignment;

        if (!_jobs.TryGetAllDepartments(jobId, out var departments))
            return null;

        departments.Sort((a, b) => b.Weight.CompareTo(a.Weight));
        foreach (var department in departments)
        {
            if (department.FirstDeploymentAssignment is { } departmentAssignment)
                return departmentAssignment;
        }

        return null;
    }

    private string FormatTime(DateTime worldDate)
    {
        return Loc.GetString("rmc-announcement-first-deployment-time",
            ("time", worldDate.ToString("HHmm", CultureInfo.InvariantCulture)),
            ("date", worldDate.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()));
    }

    private string FormatOperationName(string operationName)
    {
        const string prefix = "Operation ";
        var name = operationName.Trim();
        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            name = name[prefix.Length..].TrimStart();

        return Loc.GetString("rmc-announcement-first-deployment-operation", ("name", name)).ToUpperInvariant();
    }

    private string? GetWarshipName(EntityUid warship)
    {
        var station = _station.GetOwningStation(warship) ?? _station.GetStationInMap(Transform(warship).MapID);
        if (station is not { } stationUid)
            return null;

        var name = Name(stationUid).Trim();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private string? GetAnyWarshipName()
    {
        var query = EntityQueryEnumerator<AlmayerComponent>();
        while (query.MoveNext(out var warship, out _))
        {
            if (GetWarshipName(warship) is { } name)
                return name;
        }

        return null;
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCFirstDeploymentComponent>();
        while (query.MoveNext(out var deployment))
        {
            if (deployment.Announced ||
                deployment.AnnounceAt is not { } announceAt ||
                time < announceAt)
            {
                continue;
            }

            deployment.AnnounceAt = null;
            deployment.Announced = true;
            Announce(deployment);
        }
    }
}
