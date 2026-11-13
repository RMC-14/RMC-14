using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Xenonids.Watch;

[AnyCommand]
internal sealed class XenoWatchEntityCommand : IConsoleCommand
{
    public const string CommandName = "xeno_watch_entity";

    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => CommandName;
    public string Description => string.Empty;
    public string Help => string.Empty;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 || shell.Player is not { } player)
            return;

        if (!NetEntity.TryParse(args[0], out var targetNet))
            return;

        if (player.AttachedEntity is not { } watcher)
            return;

        var target = _entities.GetEntity(targetNet);
        var watchSystem = _entities.System<XenoWatchSystem>();

        if (_entities.TryGetComponent(watcher, out XenoWatchingComponent? watching) &&
            watching.Watching == target)
        {
            watchSystem.Unwatch(watcher, player);
        }
        else
        {
            watchSystem.Watch(watcher, target);
        }
    }
}
