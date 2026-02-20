using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Attachable;

[AdminCommand(AdminFlags.Debug)]
public sealed class ShowIFFDebugCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;

    public string Command => "showiffdebug";
    public string Description => "Toggles IFF prediction debug overlay for your client.";
    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine("You must be a player to use this command.");
            return;
        }

        var debug = _entitySystem.GetEntitySystem<AttachableIFFDebugSystem>();
        var enabled = debug.ToggleObserver(player);

        shell.WriteLine(enabled
            ? "Enabled the IFF prediction debug overlay."
            : "Disabled the IFF prediction debug overlay.");
    }
}
