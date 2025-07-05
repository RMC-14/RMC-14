using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server._RMC14.Marines;
using Content.Server._RMC14.Xenonids;
using Content.Shared._RMC14.AegisEvent;
using Content.Shared._RMC14.Requisitions.Components;
using Robust.Shared.Map;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Content.Server.Fax;
using Content.Shared.Fax.Components;
using Content.Shared.Paper;
using Robust.Shared.Localization;

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
        // Send fax to Marine High Command
        SendAegisFax(systemManager, entityManager, message);

        // Spawn and send the Aegis ID card
        var idItem = entityManager.SpawnEntity("RMCIDCardAegis", MapCoordinates.Nullspace);
        entityManager.EnsureComponent<RequisitionsCustomDeliveryComponent>(idItem);

        // Spawn and send the Powerloader pamphlet
        var pamphletItem = entityManager.SpawnEntity("CMPamphletPowerloader", MapCoordinates.Nullspace);
        entityManager.EnsureComponent<RequisitionsCustomDeliveryComponent>(pamphletItem);

        shell.WriteLine("Aegis event announced to marines and xenos, fax sent to Marine High Command, and items sent through ASRS.");
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
