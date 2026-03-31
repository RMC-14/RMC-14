using System.Globalization;
using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Dropship.Utility.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server._RMC14.Admin.Utility;

[AdminCommand(AdminFlags.VarEdit)]
internal sealed class OrbitalDropCommand : LocalizedEntityCommands
{
    public override string Command => "orbitaldrop";
    public override string Description => Loc.GetString("cmd-orbitaldrop-desc");
    public override string Help => Loc.GetString("cmd-orbitaldrop-help");

    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedRMCOrbitalDeployerSystem _orbitalDeployer = default!;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 4)
        {
            shell.WriteError(Help);
            return;
        }

        if (!NetEntity.TryParse(args[0], out var entityUidNet) || !_entities.TryGetEntity(entityUidNet, out var entity))
        {
            shell.WriteError($"Invalid entity: {args[0]}");
            return;
        }

        if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
        {
            shell.WriteError("Invalid coordinates");
            return;
        }

        if (!int.TryParse(args[3], out var mapIdInt))
        {
            shell.WriteError($"Invalid MapId: {args[3]}");
            return;
        }

        var mapId = new MapId(mapIdInt);
        if (!_mapSystem.MapExists(mapId))
        {
            shell.WriteError($"Map {mapId} does not exist!");
            return;
        }

        var dropDelay = args.Length > 4 && float.TryParse(args[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var dd) ? dd : 5f;
        var dropDuration = args.Length > 5 && float.TryParse(args[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var dur) ? dur : 3f;
        var timeToOpen = args.Length > 6 && float.TryParse(args[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var tto) ? tto : 2f;
        var randomScatter = args.Length > 7 && int.TryParse(args[7], out var sc) ? sc : 0;
        var useParachute = args.Length <= 8 || !bool.TryParse(args[8], out var parachute) || parachute;

        var coords = new MapCoordinates(x, y, mapId);
        _orbitalDeployer.DoOrbitalDeploy(entity.Value, coords, dropDelay, dropDuration, timeToOpen, randomScatter, useParachute);

        shell.WriteLine($"{entity} is being orbital dropped at {mapId}:{x},{y}.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(_entities.GetEntities().Select(e => e.ToString()), "<EntityUid>"),
            2 => CompletionResult.FromHint("<x>"),
            3 => CompletionResult.FromHint("<y>"),
            4 => CompletionResult.FromHintOptions(CompletionHelper.MapIds(_entities), "[MapId]"),
            5 => CompletionResult.FromHint("dropDelay 5 <float>"),
            6 => CompletionResult.FromHint("dropDuration 3 <float>"),
            7 => CompletionResult.FromHint("timeToOpen 2 <float>"),
            8 => CompletionResult.FromHint("randomScatter 0 <int>"),
            9 => CompletionResult.FromHint("useParachute true <bool>"),
            _ => CompletionResult.Empty,
        };
    }
}
