using System.Linq;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Shared._RMC14.Weather;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server._RMC14.Weather;

/// <summary>
///     Server-side admin command and audit announcements for manually controlling RMC weather cycles.
/// </summary>
public sealed class RMCWeatherCommandSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly RMCWeatherSystem _weather = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCWeatherStartedEvent>(OnWeatherStarted);
        SubscribeLocalEvent<RMCWeatherEndedEvent>(OnWeatherEnded);

        _console.RegisterCommand("rmcweather",
            Loc.GetString("rmc-weather-command-description"),
            Loc.GetString("rmc-weather-command-help"),
            RMCWeather,
            RMCWeatherCompletion);
    }

    private void OnWeatherStarted(ref RMCWeatherStartedEvent ev)
    {
        // Weather start/end events are raised by the shared system; the server layer turns them into admin-visible audit text.
        if (ev.Duration is { } value)
        {
            _chat.SendAdminAnnouncement(Loc.GetString(ev.AdminForced
                    ? "rmc-weather-admin-started-forced"
                    : "rmc-weather-admin-started",
                ("weather", ev.Name),
                ("map", ev.MapId),
                ("seconds", (int) value.TotalSeconds)));
            return;
        }

        _chat.SendAdminAnnouncement(Loc.GetString(ev.AdminForced
                ? "rmc-weather-admin-started-forced-permanent"
                : "rmc-weather-admin-started-permanent",
            ("weather", ev.Name),
            ("map", ev.MapId)));
    }

    private void OnWeatherEnded(ref RMCWeatherEndedEvent ev)
    {
        if (ev.Elapsed is { } value)
        {
            _chat.SendAdminAnnouncement(Loc.GetString(ev.AdminForced
                    ? "rmc-weather-admin-ended-forced-after"
                    : "rmc-weather-admin-ended-after",
                ("weather", ev.Name),
                ("map", ev.MapId),
                ("seconds", (int) value.TotalSeconds)));
            return;
        }

        _chat.SendAdminAnnouncement(Loc.GetString(ev.AdminForced
                ? "rmc-weather-admin-ended-forced"
                : "rmc-weather-admin-ended",
            ("weather", ev.Name),
            ("map", ev.MapId)));
    }

    [AdminCommand(AdminFlags.Spawn)]
    private void RMCWeather(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("rmc-weather-command-not-enough-arguments"));
            return;
        }

        if (!TryParseMap(shell, args[1], out var mapId))
            return;

        switch (args[0].ToLowerInvariant())
        {
            case "status":
                shell.WriteLine(_weather.GetWeatherStatus(mapId));
                return;

            case "end":
                WriteResult(shell, _weather.TryEndWeather(mapId, out var endMessage), endMessage);
                return;

            case "start":
                StartWeather(shell, mapId, args);
                return;

            default:
                shell.WriteError(Loc.GetString("rmc-weather-command-unknown-action", ("action", args[0])));
                return;
        }
    }

    private void StartWeather(IConsoleShell shell, MapId mapId, string[] args)
    {
        if (args.Length < 3)
        {
            shell.WriteError(Loc.GetString("rmc-weather-command-missing-event"));
            return;
        }

        // Only event indexes are accepted; a trailing "now" skips the warning.
        var eventArgs = args.Skip(2).ToList();
        var skipWarning = eventArgs.Count > 0 &&
                          string.Equals(eventArgs[^1], "now", StringComparison.OrdinalIgnoreCase);
        if (skipWarning)
            eventArgs.RemoveAt(eventArgs.Count - 1);

        if (eventArgs.Count == 0)
        {
            shell.WriteError(Loc.GetString("rmc-weather-command-missing-event"));
            return;
        }

        var eventKey = string.Join(' ', eventArgs);
        WriteResult(shell, _weather.TryStartWeatherEvent(mapId, eventKey, skipWarning, true, out var message), message);
    }

    private bool TryParseMap(IConsoleShell shell, string arg, out MapId mapId)
    {
        mapId = default;
        if (!int.TryParse(arg, out var mapInt))
        {
            shell.WriteError(Loc.GetString("rmc-weather-command-map-id-integer"));
            return false;
        }

        mapId = new MapId(mapInt);
        if (!_map.MapExists(mapId))
        {
            shell.WriteError(Loc.GetString("rmc-weather-command-map-does-not-exist", ("map", mapId)));
            return false;
        }

        return true;
    }

    private static void WriteResult(IConsoleShell shell, bool success, string message)
    {
        if (success)
            shell.WriteLine(message);
        else
            shell.WriteError(message);
    }

    private CompletionResult RMCWeatherCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(new[]
            {
                "status",
                "start",
                "end",
            }, Loc.GetString("rmc-weather-command-hint-action"));
        }

        if (args.Length == 2)
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager),
                Loc.GetString("rmc-weather-command-hint-map-id"));

        if (args.Length == 3 &&
            string.Equals(args[0], "start", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(args[1], out var mapInt))
        {
            // Options come from the target map cycle; completion shows the event behind each index.
            var options = _weather.GetWeatherEventCompletionOptions(new MapId(mapInt))
                .Select(option => new CompletionOption(option.Value, option.Hint));

            return CompletionResult.FromHintOptions(options,
                Loc.GetString("rmc-weather-command-hint-event"));
        }

        if (args.Length == 4 &&
            string.Equals(args[0], "start", StringComparison.OrdinalIgnoreCase))
        {
            return CompletionResult.FromHint(Loc.GetString("rmc-weather-command-hint-now"));
        }

        return CompletionResult.Empty;
    }
}
