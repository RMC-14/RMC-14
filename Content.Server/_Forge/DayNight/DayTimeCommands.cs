using Content.Server.Administration;
using Content.Shared._Forge.DayNight;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;

namespace Content.Server._Forge.DayNight;

internal static class DayTimeCommandHelper
{
    public static bool TryParseMapId(string arg, out MapId mapId)
    {
        mapId = default;
        if (!int.TryParse(arg, out var id))
            return false;
        mapId = new MapId(id);
        return true;
    }

    public static CompletionResult CompleteMapIds()
    {
        return CompletionResult.FromHintOptions(CompletionHelper.MapIds(IoCManager.Resolve<IEntityManager>()), "Map Id");
    }

    public static string GetPhaseName(DayPhase phase)
    {
        return phase switch
        {
            DayPhase.DeepNight => Loc.GetString("dayphase-deep-night"),
            DayPhase.Night => Loc.GetString("dayphase-night"),
            DayPhase.Dawn => Loc.GetString("dayphase-dawn"),
            DayPhase.Morning => Loc.GetString("dayphase-morning"),
            DayPhase.Day => Loc.GetString("dayphase-day"),
            DayPhase.Afternoon => Loc.GetString("dayphase-afternoon"),
            DayPhase.Evening => Loc.GetString("dayphase-evening"),
            DayPhase.LateEvening => Loc.GetString("dayphase-late-evening"),
            _ => Loc.GetString("dayphase-day"),
        };
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class GetDayPhaseCommand : IConsoleCommand
{
    public string Command => "getdayphase";
    public string Description => Loc.GetString("command-getdayphase-description");
    public string Help => Loc.GetString("command-getdayphase-help");

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return DayTimeCommandHelper.CompleteMapIds();

        return CompletionResult.Empty;
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("command-getdayphase-usage"));
            return;
        }

        if (!DayTimeCommandHelper.TryParseMapId(args[0], out var mapId))
        {
            shell.WriteError(Loc.GetString("command-getdayphase-invalid-map"));
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        var timeSys = entManager.System<DayNightTimeSystem>();

        if (!timeSys.TryGetTime(mapId, out var info))
        {
            shell.WriteError(Loc.GetString("command-getdayphase-no-cycle"));
            return;
        }

        shell.WriteLine(Loc.GetString("command-getdayphase-success",
            ("phase", DayTimeCommandHelper.GetPhaseName(info.Phase)),
            ("day", info.DayNumber)));
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class GetDayTimeCommand : IConsoleCommand
{
    public string Command => "getdaytime";
    public string Description => Loc.GetString("command-getdaytime-description");
    public string Help => Loc.GetString("command-getdaytime-help");

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return DayTimeCommandHelper.CompleteMapIds();

        return CompletionResult.Empty;
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("command-getdaytime-usage"));
            return;
        }

        if (!DayTimeCommandHelper.TryParseMapId(args[0], out var mapId))
        {
            shell.WriteError(Loc.GetString("command-getdaytime-invalid-map"));
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        var timeSys = entManager.System<DayNightTimeSystem>();

        if (!timeSys.TryGetTime(mapId, out var info))
        {
            shell.WriteError(Loc.GetString("command-getdaytime-no-cycle"));
            return;
        }

        var timeText = $"{info.Hour:D2}:{info.Minute:D2}";
        shell.WriteLine(Loc.GetString("command-getdaytime-success",
            ("time", timeText),
            ("day", info.DayNumber)));
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class GetDayInfoCommand : IConsoleCommand
{
    public string Command => "getdayinfo";
    public string Description => Loc.GetString("command-getdayinfo-description");
    public string Help => Loc.GetString("command-getdayinfo-help");

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return DayTimeCommandHelper.CompleteMapIds();

        return CompletionResult.Empty;
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("command-getdayinfo-usage"));
            return;
        }

        if (!DayTimeCommandHelper.TryParseMapId(args[0], out var mapId))
        {
            shell.WriteError(Loc.GetString("command-getdayinfo-invalid-map"));
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        var timeSys = entManager.System<DayNightTimeSystem>();

        if (!timeSys.TryGetTime(mapId, out var info))
        {
            shell.WriteError(Loc.GetString("command-getdayinfo-no-cycle"));
            return;
        }

        var timeText = $"{info.Hour:D2}:{info.Minute:D2}";
        shell.WriteLine(Loc.GetString("command-getdayinfo-success",
            ("day", info.DayNumber),
            ("phase", DayTimeCommandHelper.GetPhaseName(info.Phase)),
            ("time", timeText)));
    }
}
