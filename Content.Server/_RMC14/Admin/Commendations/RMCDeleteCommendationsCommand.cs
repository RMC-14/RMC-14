using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Admin.Commendations;

[AdminCommand(AdminFlags.Commendations)]
public sealed class RMCDeleteCommendationsCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public override string Command => "rmcdeletecommendations";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("cmd-rmcdeletecommendations-invalid-arguments"));
            shell.WriteLine(Help);
            return;
        }

        // Mode 1: id <commendationId>
        if (args[0].Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 2 || !int.TryParse(args[1], out var commendationId))
            {
                shell.WriteError(Loc.GetString("cmd-rmcdeletecommendations-invalid-id"));
                shell.WriteLine(Help);
                return;
            }

            var commendation = await _db.DeleteCommendationById(commendationId, includePlayers: true);

            if (commendation == null)
            {
                shell.WriteLine(Loc.GetString("cmd-rmcdeletecommendations-no-results"));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-rmcdeletecommendations-id-header", ("id", commendationId)));
            shell.WriteLine(FormatCommendation(commendation));
            return;
        }

        // Mode 2: round <roundId> <type> [giver|receiver] [usernameOrId]
        if (args[0].Equals("round", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 2 || !int.TryParse(args[1], out var roundId))
            {
                shell.WriteError(Loc.GetString("cmd-rmcdeletecommendations-invalid-round-id"));
                shell.WriteLine(Help);
                return;
            }

            if (args.Length < 3)
            {
                shell.WriteError(Loc.GetString("cmd-rmcdeletecommendations-invalid-arguments"));
                shell.WriteLine(Help);
                return;
            }

            var typeFilter = args[2].ToLowerInvariant();
            if (!TryParseCommendationType(typeFilter, out var filterType))
            {
                shell.WriteError(Loc.GetString("cmd-rmcdeletecommendations-invalid-type", ("type", typeFilter)));
                shell.WriteLine(Help);
                return;
            }

            Guid? giverId = null;
            Guid? receiverId = null;

            if (args.Length > 3)
            {
                if (args.Length < 5)
                {
                    shell.WriteError(Loc.GetString("cmd-rmcdeletecommendations-invalid-arguments"));
                    shell.WriteLine(Help);
                    return;
                }

                var isGiver = args[3].Equals("giver", StringComparison.OrdinalIgnoreCase);
                var isReceiver = args[3].Equals("receiver", StringComparison.OrdinalIgnoreCase);

                if (!isGiver && !isReceiver)
                {
                    shell.WriteError(Loc.GetString("cmd-rmcdeletecommendations-invalid-player-mode"));
                    shell.WriteLine(Help);
                    return;
                }

                var located = await _locator.LookupIdByNameOrIdAsync(args[4]);
                if (located == null)
                {
                    shell.WriteError(Loc.GetString("cmd-rmcdeletecommendations-player-not-found", ("player", args[4])));
                    return;
                }

                if (isGiver)
                    giverId = located.UserId.UserId;
                else
                    receiverId = located.UserId.UserId;

                if (args.Length > 5)
                {
                    shell.WriteError(Loc.GetString("cmd-rmcdeletecommendations-invalid-arguments"));
                    shell.WriteLine(Help);
                    return;
                }
            }

            var commendations = await _db.DeleteCommendationsByRound(roundId, filterType, giverId, receiverId, includePlayers: true);

            if (commendations.Count == 0)
            {
                shell.WriteLine(Loc.GetString("cmd-rmcdeletecommendations-no-results"));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-rmcdeletecommendations-round-header", ("round", roundId), ("count", commendations.Count)));

            foreach (var c in commendations.OrderBy(c => c.Id))
            {
                shell.WriteLine(FormatCommendation(c));
            }

            return;
        }

        shell.WriteError(Loc.GetString("cmd-rmcdeletecommendations-invalid-arguments"));
        shell.WriteLine(Help);
    }

    private bool TryParseCommendationType(string typeStr, out CommendationType type)
    {
        return Enum.TryParse(typeStr, ignoreCase: true, out type);
    }

    private string FormatCommendation(RMCCommendation c)
    {
        return Loc.GetString("cmd-rmcdeletecommendations-format",
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
                new CompletionOption("id", Loc.GetString("cmd-rmcdeletecommendations-hint-mode-id")),
                new CompletionOption("round", Loc.GetString("cmd-rmcdeletecommendations-hint-mode-round"))
            };
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-rmcdeletecommendations-hint-mode"));
        }

        if (args.Length == 2)
        {
            if (args[0].Equals("id", StringComparison.OrdinalIgnoreCase))
                return CompletionResult.FromHint(Loc.GetString("cmd-rmcdeletecommendations-hint-commendation-id"));

            if (args[0].Equals("round", StringComparison.OrdinalIgnoreCase))
                return CompletionResult.FromHint(Loc.GetString("cmd-rmcdeletecommendations-hint-round-id"));
        }

        if (args.Length == 3 && args[0].Equals("round", StringComparison.OrdinalIgnoreCase))
            return GetTypeOptions();

        if (args.Length == 4 && args[0].Equals("round", StringComparison.OrdinalIgnoreCase))
        {
            var options = new[]
            {
                new CompletionOption("giver", Loc.GetString("cmd-rmcdeletecommendations-hint-player-giver")),
                new CompletionOption("receiver", Loc.GetString("cmd-rmcdeletecommendations-hint-player-receiver"))
            };
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-rmcdeletecommendations-hint-player-mode"));
        }

        if (args.Length == 5 && args[0].Equals("round", StringComparison.OrdinalIgnoreCase))
        {
            if (args[3].Equals("giver", StringComparison.OrdinalIgnoreCase) ||
                args[3].Equals("receiver", StringComparison.OrdinalIgnoreCase))
            {
                var options = _players.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-rmcdeletecommendations-hint-player"));
            }
        }

        return CompletionResult.Empty;
    }

    private CompletionResult GetTypeOptions()
    {
        var options = Enum.GetNames<CommendationType>()
            .Select(x => new CompletionOption(x.ToLowerInvariant()))
            .ToArray();

        return CompletionResult.FromHintOptions(
            options,
            Loc.GetString("cmd-rmcdeletecommendations-hint-type")
        );
    }
}
