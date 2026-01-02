using Content.Server._RMC14.Announce;
using Content.Server.Administration;
using Content.Shared._RMC14.Bioscan;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Xenonids;

[AdminCommand(AdminFlags.Moderator)]
public sealed class XenoAnnounceCommand : IConsoleCommand
{
    public string Command => "xenoannounce";
    public string Description => "Announces a message to all xenos.";
    public string Help => $"Usage: {Command} message";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var xenoAnnounce = IoCManager.Resolve<IEntityManager>().System<XenoAnnounceSystem>();
        if (args.Length == 0)
        {
            shell.WriteError("Not enough arguments! Need at least 1.");
            return;
        }

        var message = string.Join(" ", args);
        xenoAnnounce.AnnounceQueenMother(message);
    }
}
