using System.Globalization;
using System.Linq;
using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Intel;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Roles;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Survivor;
using Content.Shared.Ghost;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private static readonly ProtoId<AnnouncementPresetPrototype> FirstDeploymentAnnouncementPreset = "MarineFirstDeployment";
    private static readonly EntProtoId DropshipJobsGroup = "CMJobsDropship";

    private static readonly ProtoId<DepartmentPrototype> CommandDepartment = "CMCommand";
    private static readonly ProtoId<DepartmentPrototype> EngineeringDepartment = "CMEngineering";
    private static readonly ProtoId<DepartmentPrototype> MedicalDepartment = "CMMedbay";
    private static readonly ProtoId<DepartmentPrototype> MilitaryPoliceDepartment = "CMMilitaryPolice";
    private static readonly ProtoId<DepartmentPrototype> RequisitionsDepartment = "CMRequisitions";
    private static readonly ProtoId<DepartmentPrototype> SquadDepartment = "CMSquad";

    private static readonly ProtoId<JobPrototype> ExecutiveBodyguardJob = "RMCWeYaExecutiveBodyguard";
    private static readonly ProtoId<JobPrototype> LiaisonJob = "CMLiaison";
    private static readonly ProtoId<JobPrototype> SeniorEnlistedAdvisorJob = "CMSeniorEnlistedAdvisor";
    private static readonly ProtoId<JobPrototype> VehicleCrewmanDeploymentJob = "CMVehicleCrewman";

    private void OnDropshipLaunchedFromWarship(ref DropshipLaunchedFromWarshipEvent ev)
    {
        var rule = TryGetActiveRuleEntity();
        if (rule is not { } activeRule ||
            activeRule.Comp.FirstDeploymentAnnouncementDone ||
            activeRule.Comp.FirstDeploymentAnnouncementAt != null ||
            ev.Dropship.Comp.Destination is not { } destination ||
            !IsFirstDeploymentDestination(destination))
        {
            return;
        }

        activeRule.Comp.FirstDeploymentWarshipName = GetWarshipName(ev.Warship);
        TryScheduleFirstDeploymentAnnouncement(activeRule.Comp, Timing.CurTime);
    }

    internal bool IsFirstDeploymentDestination(EntityUid destination)
    {
        return TryComp(destination, out TransformComponent? transform) && _rmcPlanet.IsOnPlanet(transform);
    }

    internal static bool TryScheduleFirstDeploymentAnnouncement(CMDistressSignalRuleComponent rule, TimeSpan time)
    {
        if (rule.FirstDeploymentAnnouncementDone || rule.FirstDeploymentAnnouncementAt != null)
            return false;

        rule.FirstDeploymentAnnouncementAt = time + rule.FirstDeploymentAnnouncementDelay;
        return true;
    }

    internal bool TryScheduleFirstDeploymentAnnouncementForDebug(out TimeSpan delay, out string? error)
    {
        delay = TimeSpan.Zero;
        error = null;

        if (TryGetActiveRuleEntity() is not { } rule)
        {
            error = "No active distress signal rule was found.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(OperationName) || string.IsNullOrWhiteSpace(SelectedPlanetMapName))
        {
            error = "The active distress signal rule has no operation or planet name.";
            return false;
        }

        rule.Comp.FirstDeploymentAnnouncementAt = null;
        rule.Comp.FirstDeploymentAnnouncementDone = false;
        rule.Comp.FirstDeploymentWarshipName = null;
        delay = rule.Comp.FirstDeploymentAnnouncementDelay;
        return TryScheduleFirstDeploymentAnnouncement(rule.Comp, Timing.CurTime);
    }

    private void UpdateFirstDeploymentAnnouncement(CMDistressSignalRuleComponent rule, TimeSpan time)
    {
        if (rule.FirstDeploymentAnnouncementDone ||
            rule.FirstDeploymentAnnouncementAt is not { } announceAt ||
            time < announceAt)
        {
            return;
        }

        rule.FirstDeploymentAnnouncementAt = null;
        rule.FirstDeploymentAnnouncementDone = true;

        if (string.IsNullOrWhiteSpace(OperationName) || string.IsNullOrWhiteSpace(SelectedPlanetMapName))
        {
            Log.Warning("Could not show the first deployment announcement because the operation or planet name was missing.");
            return;
        }

        var warshipName = rule.FirstDeploymentWarshipName;
        if (string.IsNullOrWhiteSpace(warshipName) && !TryGetActiveWarshipName(out warshipName))
        {
            Log.Warning("Could not show the first deployment announcement because the warship name was missing.");
            return;
        }

        AnnounceFirstDeployment(OperationName, SelectedPlanetMapName, warshipName);
    }

    private void AnnounceFirstDeployment(string operationName, string planetName, string warshipName)
    {
        var recipients = CollectFirstDeploymentRecipients();
        if (recipients.Count == 0)
            return;

        var time = FormatFirstDeploymentTime(_rmcClock.GetWorldTime());

        foreach (var (assignment, sessions) in recipients)
        {
            var message = Loc.GetString("rmc-announcement-first-deployment-message",
                ("operation", FormatFirstDeploymentOperationName(operationName)),
                ("time", time),
                ("planet", planetName),
                ("assignment", GetFirstDeploymentAssignmentName(assignment, warshipName)));

            var request = new AnnouncementRequest
            {
                Message = message,
                Preset = FirstDeploymentAnnouncementPreset,
                Route = new AnnouncementRoute
                {
                    Target = AnnouncementTarget.Marines,
                    Channels = AnnouncementChannels.Overlay,
                },
            };

            _announcementRouter.Announce(request, Filter.Empty().AddPlayers(sessions));
        }
    }

    private Dictionary<FirstDeploymentAssignment, List<ICommonSession>> CollectFirstDeploymentRecipients()
    {
        var recipients = new Dictionary<FirstDeploymentAssignment, List<ICommonSession>>();
        foreach (var session in Filter.Broadcast().Recipients)
        {
            if (session.AttachedEntity is not { } entity || !IsFirstDeploymentRecipient(entity))
                continue;

            ProtoId<JobPrototype>? job = null;
            if (TryComp(entity, out OriginalRoleComponent? originalRole))
                job = originalRole.Job;

            var assignment = GetFirstDeploymentAssignment(job);
            if (!recipients.TryGetValue(assignment, out var assignmentRecipients))
            {
                assignmentRecipients = [];
                recipients.Add(assignment, assignmentRecipients);
            }

            assignmentRecipients.Add(session);
        }

        return recipients;
    }

    internal bool IsFirstDeploymentRecipient(EntityUid entity)
    {
        return HasComp<MarineComponent>(entity) &&
               !HasComp<GhostComponent>(entity) &&
               !HasComp<RMCSurvivorComponent>(entity) &&
               !HasComp<IntelRescueSurvivorObjectiveComponent>(entity) &&
               TryComp(entity, out MobStateComponent? mobState) &&
               mobState.CurrentState != MobState.Dead &&
               TryComp(entity, out TransformComponent? transform) &&
               !_rmcPlanet.IsOnPlanet(transform);
    }

    internal FirstDeploymentAssignment GetFirstDeploymentAssignment(ProtoId<JobPrototype>? job)
    {
        if (job is not { } jobId)
            return FirstDeploymentAssignment.Crew;

        if (jobId == SeniorEnlistedAdvisorJob ||
            HasDepartment(jobId, CommandDepartment) ||
            HasDepartment(jobId, SquadDepartment))
        {
            return FirstDeploymentAssignment.Combat;
        }

        if (IsDropshipJob(jobId))
            return FirstDeploymentAssignment.FlightCrew;

        if (HasDepartment(jobId, MilitaryPoliceDepartment))
            return FirstDeploymentAssignment.Security;

        if (jobId == VehicleCrewmanDeploymentJob || HasDepartment(jobId, EngineeringDepartment))
            return FirstDeploymentAssignment.Engineering;

        if (HasDepartment(jobId, MedicalDepartment))
            return FirstDeploymentAssignment.Medical;

        if (HasDepartment(jobId, RequisitionsDepartment))
            return FirstDeploymentAssignment.Logistics;

        if (jobId == LiaisonJob || jobId == ExecutiveBodyguardJob)
            return FirstDeploymentAssignment.Liaison;

        return FirstDeploymentAssignment.Crew;
    }

    private bool HasDepartment(ProtoId<JobPrototype> job, ProtoId<DepartmentPrototype> expected)
    {
        if (!_jobs.TryGetAllDepartments(job, out var departments))
            return false;

        foreach (var department in departments)
        {
            if (department.ID == expected)
                return true;
        }

        return false;
    }

    private bool IsDropshipJob(ProtoId<JobPrototype> job)
    {
        return _prototypes.TryIndex(DropshipJobsGroup, out EntityPrototype? groupPrototype) &&
               groupPrototype.TryGetComponent(out JobGroupComponent? group) &&
               group.Jobs.Contains(job);
    }

    internal static string FormatFirstDeploymentTime(DateTime worldDate)
    {
        return worldDate
            .ToString("HHmm 'HRS,' dd-MMM-yyyy", CultureInfo.InvariantCulture)
            .ToUpperInvariant();
    }

    internal static string FormatFirstDeploymentOperationName(string operationName)
    {
        const string prefix = "Operation ";
        var name = operationName.Trim();
        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            name = name[prefix.Length..].TrimStart();

        return $"{prefix}{name}".ToUpperInvariant();
    }

    private string? GetWarshipName(EntityUid warship)
    {
        var station = _station.GetOwningStation(warship) ?? _station.GetStationInMap(Transform(warship).MapID);
        if (station is not { } stationUid)
            return null;

        var name = Name(stationUid).Trim();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private bool TryGetActiveWarshipName([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? name)
    {
        var query = EntityQueryEnumerator<AlmayerComponent>();
        while (query.MoveNext(out var warship, out _))
        {
            if (GetWarshipName(warship) is not { } found)
                continue;

            name = found;
            return true;
        }

        name = null;
        return false;
    }

    private string GetFirstDeploymentAssignmentName(FirstDeploymentAssignment assignment, string warshipName)
    {
        var loc = assignment switch
        {
            FirstDeploymentAssignment.Combat => "rmc-announcement-first-deployment-assignment-combat",
            FirstDeploymentAssignment.FlightCrew => "rmc-announcement-first-deployment-assignment-flight-crew",
            FirstDeploymentAssignment.Security => "rmc-announcement-first-deployment-assignment-security",
            FirstDeploymentAssignment.Engineering => "rmc-announcement-first-deployment-assignment-engineering",
            FirstDeploymentAssignment.Medical => "rmc-announcement-first-deployment-assignment-medical",
            FirstDeploymentAssignment.Logistics => "rmc-announcement-first-deployment-assignment-logistics",
            FirstDeploymentAssignment.Liaison => "rmc-announcement-first-deployment-assignment-liaison",
            _ => "rmc-announcement-first-deployment-assignment-crew",
        };

        return Loc.GetString(loc, ("warship", warshipName));
    }

    internal enum FirstDeploymentAssignment : byte
    {
        Crew,
        Combat,
        FlightCrew,
        Security,
        Engineering,
        Medical,
        Logistics,
        Liaison,
    }
}
