using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Admin;

[AdminCommand(AdminFlags.Commendations)]
public sealed class ListCommendationsCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public override string Command => "listcommendations";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("cmd-listcommendations-invalid-arguments"));
            shell.WriteLine(Help);
            return;
        }

        // Mode 1: last <count> [type]
        if (args[0].Equals("last", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 2 || !int.TryParse(args[1], out var count) || count <= 0)
            {
                shell.WriteError(Loc.GetString("cmd-listcommendations-invalid-count"));
                shell.WriteLine(Help);
                return;
            }

            var typeFilter = args.Length >= 3 ? args[2].ToLowerInvariant() : "all";
            if (!TryParseCommendationType(typeFilter, out var filterType))
            {
                shell.WriteError(Loc.GetString("cmd-listcommendations-invalid-type", ("type", typeFilter)));
                shell.WriteLine(Help);
                return;
            }

            await ListLastCommendations(shell, count, filterType);
            return;
        }

        // Mode 2: round <roundId> [type]
        if (args[0].Equals("round", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 2 || !int.TryParse(args[1], out var roundId))
            {
                shell.WriteError(Loc.GetString("cmd-listcommendations-invalid-round-id"));
                shell.WriteLine(Help);
                return;
            }

            var typeFilter = args.Length >= 3 ? args[2].ToLowerInvariant() : "all";
            if (!TryParseCommendationType(typeFilter, out var filterType))
            {
                shell.WriteError(Loc.GetString("cmd-listcommendations-invalid-type", ("type", typeFilter)));
                shell.WriteLine(Help);
                return;
            }

            await ListCommendationsByRound(shell, roundId, filterType);
            return;
        }

        // Mode 3: player giver <usernameOrId> <count> [type]
        // Mode 4: player receiver <usernameOrId> <count> [type]
        if (args[0].Equals("player", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 2)
            {
                shell.WriteError(Loc.GetString("cmd-listcommendations-invalid-arguments"));
                shell.WriteLine(Help);
                return;
            }

            var isGiver = args[1].Equals("giver", StringComparison.OrdinalIgnoreCase);
            var isReceiver = args[1].Equals("receiver", StringComparison.OrdinalIgnoreCase);

            if (!isGiver && !isReceiver)
            {
                shell.WriteError(Loc.GetString("cmd-listcommendations-invalid-player-mode"));
                shell.WriteLine(Help);
                return;
            }

            if (args.Length < 3)
            {
                shell.WriteError(Loc.GetString("cmd-listcommendations-invalid-arguments"));
                shell.WriteLine(Help);
                return;
            }

            var located = await _locator.LookupIdByNameOrIdAsync(args[2]);
            if (located == null)
            {
                shell.WriteError(Loc.GetString("cmd-listcommendations-player-not-found", ("player", args[2])));
                return;
            }

            if (args.Length < 4 || !int.TryParse(args[3], out var count) || count <= 0)
            {
                shell.WriteError(Loc.GetString("cmd-listcommendations-invalid-count"));
                shell.WriteLine(Help);
                return;
            }

            var typeFilter = args.Length >= 5 ? args[4].ToLowerInvariant() : "all";
            if (!TryParseCommendationType(typeFilter, out var filterType))
            {
                shell.WriteError(Loc.GetString("cmd-listcommendations-invalid-type", ("type", typeFilter)));
                shell.WriteLine(Help);
                return;
            }

            if (isGiver)
            {
                await ListCommendationsByGiver(shell, located.UserId.UserId, count, filterType);
            }
            else
            {
                await ListCommendationsByReceiver(shell, located.UserId.UserId, count, filterType);
            }
            return;
        }

        shell.WriteError(Loc.GetString("cmd-listcommendations-invalid-arguments"));
        shell.WriteLine(Help);
    }

    private bool TryParseCommendationType(string typeStr, out CommendationType? type)
    {
        type = null;

        if (typeStr.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Enum.TryParse<CommendationType>(typeStr, ignoreCase: true, out var parsedType))
        {
            type = parsedType;
            return true;
        }

        return false;
    }

    private async Task ListLastCommendations(IConsoleShell shell, int count, CommendationType? filterType)
    {
        var commendations = await _db.GetLastCommendations(count, filterType, includePlayers: true);

        if (commendations.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-listcommendations-no-results"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-listcommendations-last-header", ("count", commendations.Count), ("total", count)));

        foreach (var c in commendations)
        {
            shell.WriteLine(FormatCommendation(c));
        }
    }

    private async Task ListCommendationsByRound(IConsoleShell shell, int roundId, CommendationType? filterType)
    {
        var commendations = await _db.GetCommendationsByRound(roundId, includePlayers: true);

        if (filterType.HasValue)
        {
            commendations = commendations.Where(c => c.Type == filterType.Value).ToList();
        }

        if (commendations.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-listcommendations-no-results"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-listcommendations-round-header", ("round", roundId), ("count", commendations.Count)));

        foreach (var c in commendations.OrderBy(c => c.Id))
        {
            shell.WriteLine(FormatCommendation(c));
        }
    }

    private async Task ListCommendationsByGiver(IConsoleShell shell, Guid playerId, int count, CommendationType? filterType)
    {
        var commendations = await _db.GetCommendationsGiven(playerId, count, filterType, true);

        // Order by ID descending (newest first)
        commendations = commendations.OrderByDescending(c => c.Id).ToList();

        if (commendations.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-listcommendations-no-results"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-listcommendations-giver-header", ("count", commendations.Count), ("total", count)));

        foreach (var c in commendations)
        {
            shell.WriteLine(FormatCommendation(c));
        }
    }

    private async Task ListCommendationsByReceiver(IConsoleShell shell, Guid playerId, int count, CommendationType? filterType)
    {
        var commendations = await _db.GetCommendationsReceived(playerId, includePlayers: true);

        if (filterType.HasValue)
        {
            commendations = commendations.Where(c => c.Type == filterType.Value).ToList();
        }

        // Order by ID descending (newest first) and take count
        commendations = commendations.OrderByDescending(c => c.Id).Take(count).ToList();

        if (commendations.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-listcommendations-no-results"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-listcommendations-receiver-header", ("count", commendations.Count), ("total", count)));

        foreach (var c in commendations)
        {
            shell.WriteLine(FormatCommendation(c));
        }
    }

    private string FormatCommendation(RMCCommendation c)
    {
        return Loc.GetString("cmd-listcommendations-format",
            ("id", c.Id),
            ("type", c.Type.ToString().ToLowerInvariant()),
            ("name", c.Name),
            ("giverUserName", c.Giver?.LastSeenUserName ?? "Unknown"),
            ("giver", c.GiverName),
            ("receiverUserName", c.Receiver?.LastSeenUserName ?? "Unknown"),
            ("receiver", c.ReceiverName),
            ("round", c.RoundId),
            ("text", c.Text));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = new[]
            {
                new CompletionOption("last", Loc.GetString("cmd-listcommendations-hint-mode-last")),
                new CompletionOption("round", Loc.GetString("cmd-listcommendations-hint-mode-round")),
                new CompletionOption("player", Loc.GetString("cmd-listcommendations-hint-mode-player"))
            };
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-listcommendations-hint-mode"));
        }

        if (args.Length == 2)
        {
            if (args[0].Equals("last", StringComparison.OrdinalIgnoreCase))
            {
                return CompletionResult.FromHint(Loc.GetString("cmd-listcommendations-hint-count"));
            }

            if (args[0].Equals("round", StringComparison.OrdinalIgnoreCase))
            {
                return CompletionResult.FromHint(Loc.GetString("cmd-listcommendations-hint-round-id"));
            }

            if (args[0].Equals("player", StringComparison.OrdinalIgnoreCase))
            {
                var options = new[]
                {
                    new CompletionOption("giver", Loc.GetString("cmd-listcommendations-hint-player-giver")),
                    new CompletionOption("receiver", Loc.GetString("cmd-listcommendations-hint-player-receiver"))
                };
                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-listcommendations-hint-player-mode"));
            }
        }

        if (args.Length == 3)
        {
            if (args[0].Equals("last", StringComparison.OrdinalIgnoreCase))
            {
                var typeOptions = new[]
                {
                    new CompletionOption("all", Loc.GetString("cmd-listcommendations-hint-type-all")),
                    new CompletionOption("medal", Loc.GetString("cmd-listcommendations-hint-type-medal")),
                    new CompletionOption("jelly", Loc.GetString("cmd-listcommendations-hint-type-jelly"))
                };
                return CompletionResult.FromHintOptions(typeOptions, Loc.GetString("cmd-listcommendations-hint-type"));
            }

            if (args[0].Equals("round", StringComparison.OrdinalIgnoreCase))
            {
                var typeOptions = new[]
                {
                    new CompletionOption("all", Loc.GetString("cmd-listcommendations-hint-type-all")),
                    new CompletionOption("medal", Loc.GetString("cmd-listcommendations-hint-type-medal")),
                    new CompletionOption("jelly", Loc.GetString("cmd-listcommendations-hint-type-jelly"))
                };
                return CompletionResult.FromHintOptions(typeOptions, Loc.GetString("cmd-listcommendations-hint-type"));
            }

            if (args[0].Equals("player", StringComparison.OrdinalIgnoreCase))
            {
                var options = _players.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-listcommendations-hint-player"));
            }
        }

        if (args.Length == 4)
        {
            if (args[0].Equals("player", StringComparison.OrdinalIgnoreCase))
            {
                return CompletionResult.FromHint(Loc.GetString("cmd-listcommendations-hint-count"));
            }
        }

        if (args.Length == 5)
        {
            if (args[0].Equals("player", StringComparison.OrdinalIgnoreCase))
            {
                var typeOptions = new[]
                {
                    new CompletionOption("all", Loc.GetString("cmd-listcommendations-hint-type-all")),
                    new CompletionOption("medal", Loc.GetString("cmd-listcommendations-hint-type-medal")),
                    new CompletionOption("jelly", Loc.GetString("cmd-listcommendations-hint-type-jelly"))
                };
                return CompletionResult.FromHintOptions(typeOptions, Loc.GetString("cmd-listcommendations-hint-type"));
            }
        }

        return CompletionResult.Empty;
    }
}
