using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server._RMC14.TacticalMap;

[AnyCommand]
public sealed class TacticalMapReplayCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public string Command => "tacmapreplay";
    public string Description => "Opens the tactical map replay viewer for the caller.";
    public string Help => "Usage: tacmapreplay [mapId]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteLine(Help);
            return;
        }

        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine("This command can only be run by a player.");
            return;
        }

        var replay = _entitySystemManager.GetEntitySystem<TacticalMapReplaySystem>();
        if (!replay.CanAccessReplay(player, out var reason))
        {
            shell.WriteLine(reason ?? "You do not have access to the tactical map replay.");
            return;
        }

        string? mapId = args.Length == 1 ? args[0] : null;
        replay.SendReplay(player, mapId);
    }
}
