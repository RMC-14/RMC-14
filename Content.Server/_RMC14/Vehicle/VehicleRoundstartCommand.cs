using System.Linq;
using Content.Server.Administration;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Intel.Tech;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Server._RMC14.Vehicle;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class VehicleRoundstartCommand : ToolshedCommand
{
    private static readonly ProtoId<JobPrototype> VehicleCrewmanJob = "CMVehicleCrewman";
    private static readonly EntProtoId VehicleHumveeArcUnlock = "VehicleHumveeARC";
    private static readonly EntProtoId VehicleTankUnlock = "VehicleTank";

    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    [CommandImplementation("current")]
    public void TestCurrent([CommandInvocationContext] IInvocationContext ctx)
    {
        TestInternal(ctx, _players.Sessions.Count());
    }

    [CommandImplementation("test")]
    public void Test(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] int playerCount)
    {
        TestInternal(ctx, playerCount);
    }

    private void TestInternal(IInvocationContext ctx, int totalPlayers)
    {
        var threshold = _config.GetCVar(RMCCVars.RMCVehicleRoundstartThresholdPlayers);
        var crewmanSlots = totalPlayers >= threshold ? 2 : 0;
        var stationJobs = Sys<StationJobsSystem>();
        var tech = Sys<TechSystem>();
        var vehicleSupply = Sys<VehicleSupplySystem>();

        var stationsUpdated = 0;
        var query = EntityManager.EntityQueryEnumerator<StationJobsComponent, StationSpawningComponent>();
        while (query.MoveNext(out var stationId, out var jobs, out _))
        {
            if (!stationJobs.TryGetJobSlot(stationId, VehicleCrewmanJob, out _, jobs))
                continue;

            stationJobs.TrySetJobSlot(stationId, VehicleCrewmanJob, crewmanSlots, stationJobs: jobs);
            stationsUpdated++;
        }

        var tankReady = totalPlayers >= threshold;
        tech.SetVehicleUnlockOptionDisabled(VehicleHumveeArcUnlock, tankReady);

        string tankResult;
        if (tankReady)
        {
            tankResult = vehicleSupply.DebugEnsureVehicleOnAnyLift(VehicleTankUnlock, true, out var reason)
                ? "ensured on vehicle lift"
                : reason ?? "failed to ensure on vehicle lift";
        }
        else
        {
            tankResult = "not applied below threshold";
        }

        ctx.WriteLine($"Vehicle roundstart test: players={totalPlayers}, threshold={threshold}");
        ctx.WriteLine($"CMVehicleCrewman slots set to {crewmanSlots} on {stationsUpdated} station(s).");
        ctx.WriteLine($"VehicleTank: {tankResult}.");
    }
}
