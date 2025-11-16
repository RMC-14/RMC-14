using Content.Server.Administration;
using Content.Shared._NC14.DayNight;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;

namespace Content.Server._NC14.DayNight;

[AdminCommand(AdminFlags.Host)]
public sealed class GetDayInfoCommand : IConsoleCommand
{
    public string Command => "nc_dayinfo";
    public string Description => Loc.GetString("nc14-command-getdayinfo-description");
    public string Help => Loc.GetString("nc14-command-getdayinfo-help");

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
            shell.WriteError(Loc.GetString("nc14-command-getdayinfo-usage"));
            return;
        }

        if (!TryParseMapId(args[0], out var mapId))
        {
            shell.WriteError(Loc.GetString("nc14-command-getdayinfo-invalid-map"));
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        var timeSys = entManager.System<NCDayNightTimeSystem>();

        if (!timeSys.TryGetTime(mapId, out var info))
        {
            shell.WriteError(Loc.GetString("nc14-command-getdayinfo-no-cycle"));
            return;
        }

        var timeText = $"{info.Hour:D2}:{info.Minute:D2}";
        var phase = GetPhaseName(info.Phase);
        shell.WriteLine(Loc.GetString("nc14-command-getdayinfo-success",
            ("day", info.DayNumber),
            ("phase", phase),
            ("time", timeText)));
    }

    private static string GetPhaseName(NCDayPhase phase)
    {
        return phase switch
        {
            NCDayPhase.DeepNight => Loc.GetString("nc14-dayphase-deep-night"),
            NCDayPhase.Night => Loc.GetString("nc14-dayphase-night"),
            NCDayPhase.Dawn => Loc.GetString("nc14-dayphase-dawn"),
            NCDayPhase.Morning => Loc.GetString("nc14-dayphase-morning"),
            NCDayPhase.Day => Loc.GetString("nc14-dayphase-day"),
            NCDayPhase.Afternoon => Loc.GetString("nc14-dayphase-afternoon"),
            NCDayPhase.Evening => Loc.GetString("nc14-dayphase-evening"),
            NCDayPhase.LateEvening => Loc.GetString("nc14-dayphase-late-evening"),
            _ => Loc.GetString("nc14-dayphase-day"),
        };
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
