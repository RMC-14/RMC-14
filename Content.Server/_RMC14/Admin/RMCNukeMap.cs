using System.Globalization;
using Content.Server._RMC14.Nuke;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server._RMC14.Admin;

[AdminCommand(AdminFlags.Fun)]
public sealed class RMCNukeMap : LocalizedEntityCommands
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly RMCNukeSystem _nuke = default!;

    public override string Command => "nukemap";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Wrong number of args.");
            return;
        }

        var arg = args[0];
        var mapId = new MapId(int.Parse(arg, CultureInfo.InvariantCulture));

        if (!_mapSystem.MapExists(mapId))
        {
            shell.WriteError("Map does not exist!");
            return;
        }

        _nuke.NukeMap(mapId);
    }
}
