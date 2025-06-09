using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server._RMC14.Marines;
using Content.Server._RMC14.Xenonids;
using Content.Shared._RMC14.AegisEvent;
using Content.Shared._RMC14.Requisitions.Components;
using Robust.Shared.Map;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class AegisEventCommand : IConsoleCommand
{
    public string Command => "aegisevent";
    public string Description => "Announces an Aegis event to both marines and xenos and sends an item through ASRS.";
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

        // Send item through ASRS (example: "AegisSpecialItem")

        var item = entityManager.SpawnEntity("RMCIDCardAegis", MapCoordinates.Nullspace);
        entityManager.EnsureComponent<RequisitionsCustomDeliveryComponent>(item);

        shell.WriteLine("Aegis event announced to marines and xenos, and item sent through ASRS.");
    }
}
