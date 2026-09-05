using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Robust.Shared.Utility;

namespace Content.Server.Station.Systems;

public sealed partial class StationJobsSystem : EntitySystem
{
    private void OnCollectingAssignments(CollectingAssignmentsEvent ev)
    {
        // Set assignment limits based on setup available jobs.
        // This is run on CollectingAssignmentsEvent rather than InitializingAssignmentsEvent because
        // systems in RMC modify the number of available slots based on the number of players.
        var stations = EntityQueryEnumerator<StationJobsComponent, StationSpawningComponent>();
        while (stations.MoveNext(out var stationId, out var stationJobs, out _))
        {
            foreach (var (jobId, available) in stationJobs.SetupAvailableJobs)
            {
                var assignments = ev.JobAssignments.GetOrNew(jobId, out var exists);

                // TODO RMC14 separate job assignments by squad
                if (!exists)
                    assignments.Add(new JobAssignment(_prototypeManager.Index(jobId), stationId));

                Log.Debug($"Setting assignment limit for {jobId} based on setup info {available}");

                foreach (var assignment in assignments)
                {
                    var limit = available[0];
                    assignment.AssignmentLimit = limit != -1 ? limit : null;
                }
            }
        }
    }
}
