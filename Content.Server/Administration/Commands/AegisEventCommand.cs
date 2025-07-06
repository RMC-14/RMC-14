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
using Content.Server.GameTicking.Events;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class AegisEventCommand : IConsoleCommand
{
    public string Command => "aegisevent";
    public string Description => "Announces the AEGIS event to both marines and xenos and sends an item through ASRS.";
    public string Help => $"Usage: {Command} <message>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError("Not enough arguments! Need at least 1.");
            return;
        }

        var systemManager = IoCManager.Resolve<IEntitySystemManager>();
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var message = string.Join(" ", args);

        // Announce to both marines and xenos
        AegisSharedAnnouncement.AnnounceToBoth(systemManager, message);

        // Spawn and send the Aegis ID card
        var idItem = entityManager.SpawnEntity("RMCIDCardAegis", MapCoordinates.Nullspace);
        entityManager.EnsureComponent<RequisitionsCustomDeliveryComponent>(idItem);

        // Spawn and send the Powerloader pamphlet
        var pamphletItem = entityManager.SpawnEntity("CMPamphletPowerloader", MapCoordinates.Nullspace);
        entityManager.EnsureComponent<RequisitionsCustomDeliveryComponent>(pamphletItem);

        shell.WriteLine("Aegis event announced to marines and xenos, and items sent through ASRS.");
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class AegisSpawnCommand : IConsoleCommand
{
    public string Command => "aegisspawn";
    public string Description => "Activates AEGIS crate spawners for this round (persists until round ends).";
    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var systemManager = IoCManager.Resolve<IEntitySystemManager>();
        var aegisSystem = systemManager.GetEntitySystem<AegisSpawnerSystem>();

        if (aegisSystem.AreAegisSpawnersScheduled())
        {
            shell.WriteLine("AEGIS spawners are already activated for this round.");
            return;
        }

        aegisSystem.SetAegisSpawnersForThisRound();
        shell.WriteLine("AEGIS spawners activated for this round. They will spawn when detected on the map.");
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class AegisStatusCommand : IConsoleCommand
{
    public string Command => "aegisstatus";
    public string Description => "Shows the current status of AEGIS spawner flags.";
    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var systemManager = IoCManager.Resolve<IEntitySystemManager>();
        var aegisSystem = systemManager.GetEntitySystem<AegisSpawnerSystem>();

        var isScheduled = aegisSystem.AreAegisSpawnersScheduled();
        var haveSpawned = aegisSystem.HaveAegisCratesSpawned();

        shell.WriteLine($"AEGIS spawners activated: {isScheduled}");
        shell.WriteLine($"AEGIS crates spawned this round: {haveSpawned}");

        // Count spawners on map
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var spawnerCount = 0;
        var aegisQuery = entityManager.EntityQueryEnumerator<AegisSpawnerComponent>();
        while (aegisQuery.MoveNext(out var uid, out var spawner))
        {
            if (!entityManager.Deleted(uid))
                spawnerCount++;
        }

        shell.WriteLine($"AEGIS spawners on map: {spawnerCount}");
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class AegisResetCommand : IConsoleCommand
{
    public string Command => "aegisreset";
    public string Description => "Resets AEGIS spawner flags (for debugging/testing purposes).";
    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var systemManager = IoCManager.Resolve<IEntitySystemManager>();
        var aegisSystem = systemManager.GetEntitySystem<AegisSpawnerSystem>();

        aegisSystem.ResetAegisSpawners();
        shell.WriteLine("AEGIS spawner flags have been reset.");
    }
}
