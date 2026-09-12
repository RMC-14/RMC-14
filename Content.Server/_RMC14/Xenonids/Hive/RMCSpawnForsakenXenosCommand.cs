using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Xenonids.Hive;

[AdminCommand(AdminFlags.Debug)]
public sealed class RMCSpawnForsakenXenosCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystems = default!;

    public string Command => "rmcspawnforsakenxenos";

    public string Description => "spawns batch of Forsaken xenos";

    public string Help => "rmcspawnforsakenxenos <count>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var count = 1;
        if (args.Length > 0 && !int.TryParse(args[0], out count))
        {
            shell.WriteError($"Invalid count \"{args[0]}\".");
            return;
        }

        var system = _entitySystems.GetEntitySystem<ForsakenXenoSystem>();
        var spawned = system.SpawnForsakenXenos(count);

        if (spawned.Count == 0)
        {
            shell.WriteLine("no Forsaken hive slot or no xeno spawn points found");
            return;
        }

        shell.WriteLine($"Spawned {spawned.Count} Forsaken xeno(s)");
    }
}
