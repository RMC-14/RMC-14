using System.Globalization;
using Content.Shared._RMC14.CameraShake;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Admin;

[AdminCommand(AdminFlags.Fun)]
public sealed class RMCShakeMap : LocalizedEntityCommands
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly RMCCameraShakeSystem _rmcCameraShake = default!;

    public override string Command => "shakemap";
    public string Description => Loc.GetString("cmd-shakemap-description");
    public string Help => Loc.GetString("cmd-shakemap-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || args.Length > 4)
        {
            shell.WriteError("Wrong number of arguments!");
            return;
        }

        var mapId = new MapId(int.Parse(args[0], CultureInfo.InvariantCulture));

        if (!_mapSystem.MapExists(mapId))
        {
            shell.WriteError("Map does not exist!");
            return;
        }

        var victims = Filter.BroadcastMap(mapId);

        var shakes = 1;
        if (args.Length > 1)
        {
            shakes = int.Parse(args[1], CultureInfo.InvariantCulture);
        }

        var strength = 10;
        if (args.Length > 2)
        {
            strength = int.Parse(args[2], CultureInfo.InvariantCulture);
        }

        _rmcCameraShake.ShakeCamera(victims, shakes, strength);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint("mapID");

        if (args.Length == 2)
            return CompletionResult.FromHint("number of shakes");

        if (args.Length == 3)
            return CompletionResult.FromHint("shake strength");

        return CompletionResult.Empty;
    }
}
