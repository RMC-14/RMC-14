using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Marines;

[AdminCommand(AdminFlags.Moderator)]
public sealed class MarineAnnounceCommand : IConsoleCommand
{
    public string Command => "marineannounce";
    public string Description => Loc.GetString("rmc-command-marineannounce-description");
    public string Help => Loc.GetString("rmc-command-marineannounce-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var marineAnnounce = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<MarineAnnounceSystem>();
        if (args.Length == 0)
        {
            shell.WriteError("Not enough arguments! Need at least 1.");
            return;
        }

        if (args.Length == 1)
        {
            marineAnnounce.AnnounceHighCommand(args[0]);
        }
        else
        {
            var message = string.Join(' ', args[1..]);
            marineAnnounce.AnnounceHighCommand(message, args[0]);
        }
        shell.WriteLine("Sent!");
    }
}
