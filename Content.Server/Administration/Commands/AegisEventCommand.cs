using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server._RMC14.Spawners;
using Content.Shared._RMC14.AegisEvent;
using Content.Shared._RMC14.Requisitions;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class AegisEventCommand : IConsoleCommand
{
    public string Command => "aegis:normal";
    public string Description => "Starts an AEGIS event immediately. Sends a fax to CIC and an AEGIS keycard and powerloader pamphlet will arrive through ASRS. You still need to spawn the crate yourself.";
    public string Help => $"Usage: {Command} [optional message]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var systemManager = IoCManager.Resolve<IEntitySystemManager>();
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var reqSystem = systemManager.GetEntitySystem<SharedRequisitionsSystem>();
        var aegisSystem = systemManager.GetEntitySystem<AegisLobbyEventSystem>();
        var message = args.Length > 0 ? string.Join(" ", args) : "AEGIS event has been initiated.";

        // Announce to both marines and xenos
        AegisSharedAnnouncement.AnnounceToBoth(systemManager, message);
        // Send fax to CIC
        aegisSystem.SendCICFax(systemManager, entityManager, message, "RMCPaperAegisInfoFax", "UNS Oberon");

        // Spawn and send the Aegis ID card
        reqSystem.CreateSpecialDelivery("RMCIDCardAegis");

        // Spawn and send the Powerloader pamphlet
        reqSystem.CreateSpecialDelivery("CMPamphletPowerloader");

        shell.WriteLine("Aegis event announced to marines and xenos, fax sent to CiC, and items sent through ASRS.");
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
