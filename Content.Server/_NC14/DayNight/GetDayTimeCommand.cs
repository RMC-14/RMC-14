using Content.Server.Administration;
using Content.Shared._NC14.DayNight;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;

namespace Content.Server._NC14.DayNight;

[AdminCommand(AdminFlags.Host)]
public sealed class GetDayTimeCommand : IConsoleCommand
{
    public string Command => "nc_getdaytime";
    public string Description => Loc.GetString("nc14-command-getdaytime-description");
    public string Help => Loc.GetString("nc14-command-getdaytime-help");

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(IoCManager.Resolve<IEntityManager>()), "Map Id");

        return CompletionResult.Empty;
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("nc14-command-getdaytime-usage"));
            return;
        }

        if (!TryParseMapId(args[0], out var mapId))
        {
            shell.WriteError(Loc.GetString("nc14-command-getdaytime-invalid-map"));
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        var timeSys = entManager.System<NCDayNightTimeSystem>();

        if (!timeSys.TryGetTime(mapId, out var info))
        {
            shell.WriteError(Loc.GetString("nc14-command-getdaytime-no-cycle"));
            return;
        }

        var timeText = $"{info.Hour:D2}:{info.Minute:D2}";
        shell.WriteLine(Loc.GetString("nc14-command-getdaytime-success",
            ("time", timeText),
            ("day", info.DayNumber)));
    }

    private static bool TryParseMapId(string arg, out MapId mapId)
    {
        mapId = default;
        if (!int.TryParse(arg, out var id))
            return false;
        mapId = new MapId(id);
        return true;
    }
}
