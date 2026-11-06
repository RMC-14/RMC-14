using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Ghost;

[AnyCommand]
internal sealed class GhostFollowEntityCommand : IConsoleCommand
{
    public const string CommandName = "ghost_follow_entity";

    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => CommandName;
    public string Description => string.Empty;
    public string Help => string.Empty;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 || shell.Player is not { } player)
            return;

        if (!NetEntity.TryParse(args[0], out var targetEnt))
            return;

        _entities.System<GhostSystem>().GhostWarpRequest(player, targetEnt);
    }
}
