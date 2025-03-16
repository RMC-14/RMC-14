using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Audio; // Add this import

namespace Content.Server._RMC14.Marines;

[AdminCommand(AdminFlags.Moderator)]
public sealed class AresAnnounceCommand : IConsoleCommand
{
    public string Command => "aresannounce";
    public string Description => Loc.GetString("rmc-command-aresannounce-description");
    public string Help => Loc.GetString("rmc-command-aresannounce-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var marineAnnounce = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<MarineAnnounceSystem>();
        if (args.Length == 0)
        {
            shell.WriteError("Not enough arguments! Need at least 1.");
            return;
        }

        var message = string.Join(' ', args);
        var localizedMessage = Loc.GetString("rmc-announcement-ares-command", ("message", message));
        var soundSpecifier = new SoundPathSpecifier("Resources/Audio/RMC14/AI/announce.ogg"); // Update this path to your actual sound file
        marineAnnounce.AnnounceARES(null, localizedMessage, soundSpecifier);
        shell.WriteLine("Sent!");
    }
}
