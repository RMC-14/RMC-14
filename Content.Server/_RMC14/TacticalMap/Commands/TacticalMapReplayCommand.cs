using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.TacticalMap;

[AnyCommand]
public sealed class TacticalMapReplayCommand : LocalizedEntityCommands
{
    [Dependency] private readonly TacticalMapReplaySystem _replay = default!;

    public override string Command => "tacmapreplay";

    public override string Description => "Opens the tactical map replay for the current round.";

    public override string Help => "Usage: tacmapreplay [map name]";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteLine(Help);
            return;
        }

        if (shell.Player is not { } player)
        {
            shell.WriteLine(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var mapName = args.Length == 1 ? args[0] : null;
        if (!_replay.TryRequestReplay(player, mapName, out var reason))
            shell.WriteLine(reason ?? "Unable to open the tactical map replay.");
    }
}
