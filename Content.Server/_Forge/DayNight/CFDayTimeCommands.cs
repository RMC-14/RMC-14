using Content.Server.Administration;
using Content.Shared._Forge.DayNight;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;

namespace Content.Server._Forge.DayNight;

internal static class CFDayTimeCommandHelper
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

    public static string GetPhaseName(CFDayPhase phase)
    {
        return phase switch
        {
            CFDayPhase.DeepNight => Loc.GetString("dayphase-deep-night"),
            CFDayPhase.Night => Loc.GetString("dayphase-night"),
            CFDayPhase.Dawn => Loc.GetString("dayphase-dawn"),
            CFDayPhase.Morning => Loc.GetString("dayphase-morning"),
            CFDayPhase.Day => Loc.GetString("dayphase-day"),
            CFDayPhase.Afternoon => Loc.GetString("dayphase-afternoon"),
            CFDayPhase.Evening => Loc.GetString("dayphase-evening"),
            CFDayPhase.LateEvening => Loc.GetString("dayphase-late-evening"),
            _ => Loc.GetString("dayphase-day"),
        };
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class CFGetDayPhaseCommand : IConsoleCommand
{
    public string Command => "cfgetdayphase";
    public string Description => Loc.GetString("command-cfgetdayphase-description");
    public string Help => Loc.GetString("command-cfgetdayphase-help");

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CFDayTimeCommandHelper.CompleteMapIds();

        return CompletionResult.Empty;
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("command-cfgetdayphase-usage"));
            return;
        }

        if (!CFDayTimeCommandHelper.TryParseMapId(args[0], out var mapId))
        {
            shell.WriteError(Loc.GetString("command-cfgetdayphase-invalid-map"));
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        var timeSys = entManager.System<CFDayNightTimeSystem>();

        if (!timeSys.TryGetTime(mapId, out var info))
        {
            shell.WriteError(Loc.GetString("command-cfgetdayphase-no-cycle"));
            return;
        }

        shell.WriteLine(Loc.GetString("command-cfgetdayphase-success",
            ("phase", CFDayTimeCommandHelper.GetPhaseName(info.Phase)),
            ("day", info.DayNumber)));
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class CFGetDayTimeCommand : IConsoleCommand
{
    public string Command => "cfgetdaytime";
    public string Description => Loc.GetString("command-cfgetdaytime-description");
    public string Help => Loc.GetString("command-cfgetdaytime-help");

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CFDayTimeCommandHelper.CompleteMapIds();

        return CompletionResult.Empty;
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("command-cfgetdaytime-usage"));
            return;
        }

        if (!CFDayTimeCommandHelper.TryParseMapId(args[0], out var mapId))
        {
            shell.WriteError(Loc.GetString("command-cfgetdaytime-invalid-map"));
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        var timeSys = entManager.System<CFDayNightTimeSystem>();

        if (!timeSys.TryGetTime(mapId, out var info))
        {
            shell.WriteError(Loc.GetString("command-cfgetdaytime-no-cycle"));
            return;
        }

        var timeText = $"{info.Hour:D2}:{info.Minute:D2}";
        shell.WriteLine(Loc.GetString("command-cfgetdaytime-success",
            ("time", timeText),
            ("day", info.DayNumber)));
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class CFGetDayInfoCommand : IConsoleCommand
{
    public string Command => "cfgetdayinfo";
    public string Description => Loc.GetString("command-cfgetdayinfo-description");
    public string Help => Loc.GetString("command-cfgetdayinfo-help");

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CFDayTimeCommandHelper.CompleteMapIds();

        return CompletionResult.Empty;
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("command-cfgetdayinfo-usage"));
            return;
        }

        if (!CFDayTimeCommandHelper.TryParseMapId(args[0], out var mapId))
        {
            shell.WriteError(Loc.GetString("command-cfgetdayinfo-invalid-map"));
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        var timeSys = entManager.System<CFDayNightTimeSystem>();

        if (!timeSys.TryGetTime(mapId, out var info))
        {
            shell.WriteError(Loc.GetString("command-cfgetdayinfo-no-cycle"));
            return;
        }

        var timeText = $"{info.Hour:D2}:{info.Minute:D2}";
        shell.WriteLine(Loc.GetString("command-cfgetdayinfo-success",
            ("day", info.DayNumber),
            ("phase", CFDayTimeCommandHelper.GetPhaseName(info.Phase)),
            ("time", timeText)));
    }
}
