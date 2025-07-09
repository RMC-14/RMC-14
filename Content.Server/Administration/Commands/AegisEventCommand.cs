using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server._RMC14.Marines;
using Content.Server._RMC14.Xenonids;
using Content.Server._RMC14.Spawners;
using Content.Shared._RMC14.AegisEvent;
using Content.Shared._RMC14.Requisitions.Components;
using Robust.Shared.Map;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Content.Server.Fax;
using Content.Shared.Fax.Components;
using Content.Shared.Paper;
using Robust.Shared.Localization;
using Content.Server.GameTicking.Events;
using Robust.Shared.Timing;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class AegisEventCommand : IConsoleCommand
{
    public string Command => "aegis:normal";
    public string Description => "Sends announcements for both sides, sends the fax to CiC and then aegis keycard and powerloader pamphlet through ASRS. You still need to spawn the crate yourself.";
    public string Help => $"Usage: {Command} [optional message]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var systemManager = IoCManager.Resolve<IEntitySystemManager>();
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var message = args.Length > 0 ? string.Join(" ", args) : "AEGIS event has been initiated.";

        // Announce to both marines and xenos
        AegisSharedAnnouncement.AnnounceToBoth(systemManager, message);
        // Send fax to Marine High Command
        SendAegisFax(systemManager, entityManager, message);

        // Spawn and send the Aegis ID card
        var idItem = entityManager.SpawnEntity("RMCIDCardAegis", MapCoordinates.Nullspace);
        entityManager.EnsureComponent<RequisitionsCustomDeliveryComponent>(idItem);

        // Spawn and send the Powerloader pamphlet
        var pamphletItem = entityManager.SpawnEntity("CMPamphletPowerloader", MapCoordinates.Nullspace);
        entityManager.EnsureComponent<RequisitionsCustomDeliveryComponent>(pamphletItem);

        shell.WriteLine("Aegis event announced to marines and xenos, fax sent to CiC, and items sent through ASRS.");
    }

    private void SendAegisFax(IEntitySystemManager systemManager, IEntityManager entityManager, string message)
    {
        var faxSystem = systemManager.GetEntitySystem<FaxSystem>();

        var faxQuery = entityManager.EntityQueryEnumerator<FaxMachineComponent>();
        while (faxQuery.MoveNext(out var faxEnt, out var faxComp))
        {
            if (faxComp.FaxName == "CIC")
            {
                var aegisPaper = entityManager.SpawnEntity("CMPaperAegisInfoFax", MapCoordinates.Nullspace);

                if (entityManager.TryGetComponent<PaperComponent>(aegisPaper, out var paperComp) &&
                    entityManager.TryGetComponent<MetaDataComponent>(aegisPaper, out var metaComp))
                {
                    var printout = new FaxPrintout(
                        paperComp.Content,
                        metaComp.EntityName,
                        null, // No label
                        "CMPaperAegisInfoFax",
                        paperComp.StampState,
                        paperComp.StampedBy
                    );

                    faxSystem.Receive(faxEnt, printout, null, faxComp);
                }

                entityManager.DeleteEntity(aegisPaper);
                break;
            }
        }
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class AegisSpawnCommand : IConsoleCommand
{
    public string Command => "aegis:lobby";
    public string Description => "Schedules the AEGIS event for the next round. Announcements, fax, and ASRS delivery will happen automatically 1 minute after round starts.";
    public string Help => $"Usage: {Command} [optional message]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var systemManager = IoCManager.Resolve<IEntitySystemManager>();
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var aegisSystem = systemManager.GetEntitySystem<AegisSpawnerSystem>();
        var aegisCorpseSystem = systemManager.GetEntitySystem<AegisCorpseSpawnerSystem>();
        var aegisEventSystem = systemManager.GetEntitySystem<AegisLobbyEventSystem>();

        if (aegisSystem.AreAegisSpawnersScheduled())
        {
            shell.WriteLine("AEGIS spawners are already activated for this round.");
            return;
        }

        var message = args.Length > 0 ? string.Join(" ", args) : "AEGIS event has been scheduled for this round.";

        // Set spawner flags to trigger at round start
        aegisSystem.SetAegisSpawnersForThisRound();
        aegisCorpseSystem.SetAegisCorpseSpawnersForThisRound();

        // Schedule only the announcements, fax, and ASRS delivery to trigger 1 minute after round start
        aegisEventSystem.ScheduleAegisEvent(message);

        shell.WriteLine("AEGIS spawners scheduled for round start. Announcements, fax, and ASRS delivery will happen 1 minute after round start.");
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class AegisStatusCommand : IConsoleCommand
{
    public string Command => "aegis:status";
    public string Description => "Shows the current status of AEGIS spawner flags.";
    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var systemManager = IoCManager.Resolve<IEntitySystemManager>();
        var aegisSystem = systemManager.GetEntitySystem<AegisSpawnerSystem>();
        var aegisCorpseSystem = systemManager.GetEntitySystem<AegisCorpseSpawnerSystem>();
        var aegisEventSystem = systemManager.GetEntitySystem<AegisLobbyEventSystem>();

        var isScheduled = aegisSystem.AreAegisSpawnersScheduled();
        var haveSpawned = aegisSystem.HaveAegisCratesSpawned();
        var areCorpseSpawnersScheduled = aegisCorpseSystem.AreAegisCorpseSpawnersScheduled();
        var haveCorpseSpawned = aegisCorpseSystem.HaveAegisCorpseSpawnersSpawned();
        var isLobbyEventScheduled = aegisEventSystem.IsEventScheduled();
        var hasLobbyEventExecuted = aegisEventSystem.HasEventExecuted();

        shell.WriteLine($"AEGIS crate spawners activated: {isScheduled}");
        shell.WriteLine($"AEGIS crates spawned this round: {haveSpawned}");
        shell.WriteLine($"AEGIS corpse spawners activated: {areCorpseSpawnersScheduled}");
        shell.WriteLine($"AEGIS corpse spawners spawned this round: {haveCorpseSpawned}");
        shell.WriteLine($"AEGIS lobby event scheduled: {isLobbyEventScheduled}");
        shell.WriteLine($"AEGIS lobby event executed: {hasLobbyEventExecuted}");

        // Count spawners on map
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var spawnerCount = 0;
        var aegisQuery = entityManager.EntityQueryEnumerator<AegisSpawnerComponent>();
        while (aegisQuery.MoveNext(out var uid, out var spawner))
        {
            if (!entityManager.Deleted(uid))
                spawnerCount++;
        }

        var corpseSpawnerCount = 0;
        var aegisCorpseQuery = entityManager.EntityQueryEnumerator<AegisCorpseSpawnerComponent>();
        while (aegisCorpseQuery.MoveNext(out var uid, out var spawner))
        {
            if (!entityManager.Deleted(uid))
                corpseSpawnerCount++;
        }

        // Count regular corpse spawners that might trigger
        var regularCorpseSpawnerCount = 0;
        var corpseQuery = entityManager.EntityQueryEnumerator<CorpseSpawnerComponent>();
        while (corpseQuery.MoveNext(out var uid, out var corpseSpawner))
        {
            if (!entityManager.Deleted(uid))
                regularCorpseSpawnerCount++;
        }
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class AegisResetCommand : IConsoleCommand
{
    public string Command => "aegis:reset";
    public string Description => "Cancels the AEGIS event for the next round.";
    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var systemManager = IoCManager.Resolve<IEntitySystemManager>();
        var aegisSystem = systemManager.GetEntitySystem<AegisSpawnerSystem>();
        var aegisCorpseSystem = systemManager.GetEntitySystem<AegisCorpseSpawnerSystem>();
        var aegisEventSystem = systemManager.GetEntitySystem<AegisLobbyEventSystem>();

        aegisSystem.ResetAegisSpawners();
        aegisCorpseSystem.ResetAegisCorpseSpawners();
        aegisEventSystem.ResetScheduledEvent();
        shell.WriteLine("AEGIS crate and corpse spawner flags have been reset, and any scheduled lobby events have been cancelled.");
    }
}
