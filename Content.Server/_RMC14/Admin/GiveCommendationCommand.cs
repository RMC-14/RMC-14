using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Dataset;
using Content.Shared.Mind;
using Content.Shared._RMC14.Commendations;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Server._RMC14.Commendations;

namespace Content.Server._RMC14.Admin;

[AdminCommand(AdminFlags.Spawn)]
public sealed class GiveCommendationCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly CommendationManager _commendation = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IChatManager _chat = default!;

    private LocalizedDatasetPrototype? _medalsDataset;
    private LocalizedDatasetPrototype? _jelliesDataset;

    public override string Command => "givecommendation";

    private int MedalCount => _medalsDataset?.Values.Count ?? 0;
    private int JellyCount => _jelliesDataset?.Values.Count ?? 0;

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        // Initialize datasets on first use
        _medalsDataset ??= _prototype.Index<LocalizedDatasetPrototype>("RMCMarineMedals");
        _jelliesDataset ??= _prototype.Index<LocalizedDatasetPrototype>("RMCXenoJellies");

        if (args.Length < 6)
        {
            shell.WriteError(Loc.GetString("cmd-givecommendation-invalid-arguments"));
            shell.WriteLine(Help);
            return;
        }

        var giverName = args[0];
        var receiverNameOrId = args[1];
        var receiverName = args[2];
        var commendationTypeStr = args[3].ToLowerInvariant();
        var awardTypeStr = args[4];
        var citation = args[5];

        // Check if last argument is a round number (optional)
        var gameTicker = _systems.GetEntitySystem<GameTicker>();
        var currentRound = gameTicker.RoundId;
        int targetRound = currentRound;

        if (args.Length == 7 && int.TryParse(args[6], out var parsedRound))
        {
            targetRound = parsedRound;
        }

        // Parse commendation type and get dataset
        CommendationType commendationType;
        LocalizedDatasetPrototype dataset;
        int maxAwardType;

        switch (commendationTypeStr)
        {
            case "medal":
                commendationType = CommendationType.Medal;
                dataset = _medalsDataset;
                maxAwardType = MedalCount;
                break;
            case "jelly":
                commendationType = CommendationType.Jelly;
                dataset = _jelliesDataset;
                maxAwardType = JellyCount;
                break;
            default:
                shell.WriteError(Loc.GetString("cmd-givecommendation-invalid-type"));
                shell.WriteLine(Help);
                return;
        }

        // Parse award type number (1-indexed, but dataset is 0-indexed)
        if (!int.TryParse(awardTypeStr, out var awardNum) || awardNum < 1 || awardNum > maxAwardType)
        {
            shell.WriteError(Loc.GetString("cmd-givecommendation-invalid-award-type",
                ("type", commendationTypeStr), ("max", maxAwardType)));
            shell.WriteLine(Help);
            return;
        }

        // Get localized name from dataset (awardNum is 1-indexed, dataset is 0-indexed)
        var locId = dataset.Values[awardNum - 1];
        var awardName = Loc.GetString(locId);

        // Validate citation
        citation = citation.Trim();
        if (string.IsNullOrWhiteSpace(citation))
        {
            shell.WriteError(Loc.GetString("cmd-givecommendation-empty-citation"));
            return;
        }

        // Use IPlayerLocator supports both username and Guid
        var located = await _locator.LookupIdByNameOrIdAsync(receiverNameOrId);

        if (located == null)
        {
            shell.WriteError(Loc.GetString("cmd-givecommendation-player-not-found", ("player", receiverNameOrId)));
            return;
        }

        var receiverId = located.UserId.UserId;

        // Get admin info
        var adminId = shell.Player?.UserId.UserId ?? Guid.Empty;

        try
        {
            // Save to database
            await _db.AddCommendation(adminId, receiverId, giverName, receiverName, awardName, citation, commendationType, targetRound);

            var commendation = new Commendation(giverName, receiverName, awardName, citation, commendationType, targetRound);
            _commendation.CommendationAdded(new NetUserId(adminId), new NetUserId(receiverId), commendation);

            // Add to round commendations only if it's for the current round
            if (targetRound == currentRound)
            {
                var commendationSystem = _systems.GetEntitySystem<CommendationSystem>();
                commendationSystem.AddRoundCommendation(commendation);
            }

            // Log admin action
            var typeName = commendationType == CommendationType.Medal ? "medal" : "jelly";
            var actualAdminName = shell.Player?.Name ?? "Server";
            var receiverLogin = located.Username;

            _adminLog.Add(LogType.RMCMedal,
                $"{actualAdminName} gave a {typeName} '{awardName}' to {receiverLogin} (character: {receiverName}) that reads:\n{citation}");

            // Send admin announcement
            var announcementMsg = Loc.GetString("cmd-givecommendation-admin-announcement",
                ("admin", actualAdminName),
                ("type", typeName),
                ("award", awardName),
                ("receiver", receiverLogin),
                ("character", receiverName),
                ("round", targetRound));
            _chat.SendAdminAnnouncement(announcementMsg, null, null);

            shell.WriteLine(Loc.GetString("cmd-givecommendation-success",
                ("award", awardName), ("player", receiverLogin)));
        }
        catch (Exception e)
        {
            Logger.Error($"Error saving commendation: {e}");
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        // Initialize datasets if needed
        _medalsDataset ??= _prototype.Index<LocalizedDatasetPrototype>("RMCMarineMedals");
        _jelliesDataset ??= _prototype.Index<LocalizedDatasetPrototype>("RMCXenoJellies");

        if (args.Length == 1)
        {
            // Complete giver name - suggest standard giver names (with quotes since they contain spaces)
            var highCommandName = Loc.GetString("rmc-announcement-author-highcommand");
            var queenMotherName = Loc.GetString("rmc-announcement-author-queen-mother");

            var options = new[]
            {
                new CompletionOption(highCommandName, Loc.GetString("cmd-givecommendation-hint-giver-highcommand")),
                new CompletionOption(queenMotherName, Loc.GetString("cmd-givecommendation-hint-giver-queen-mother"))
            };
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-givecommendation-hint-giver"));
        }

        if (args.Length == 2)
        {
            // Complete receiver username/UserId
            var options = _players.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-givecommendation-hint-receiver"));
        }

        if (args.Length == 3)
        {
            // Complete receiver character name from Mind (with quotes since names may contain spaces)
            var receiverNameOrId = args[1];
            var mindSystem = _systems.GetEntitySystem<SharedMindSystem>();

            // Try to find player and get their character name
            var characterNames = new List<CompletionOption>();

            foreach (var session in _players.Sessions)
            {
                if (!session.Name.Contains(receiverNameOrId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (mindSystem.TryGetMind(session, out _, out var mind) && !string.IsNullOrWhiteSpace(mind.CharacterName))
                {
                    characterNames.Add(new CompletionOption(mind.CharacterName, $"{session.Name} as {mind.CharacterName}"));
                }
            }

            if (characterNames.Count > 0)
                return CompletionResult.FromHintOptions(characterNames, Loc.GetString("cmd-givecommendation-hint-receiver-name"));

            return CompletionResult.FromHint(Loc.GetString("cmd-givecommendation-hint-receiver-name"));
        }

        if (args.Length == 4)
        {
            // Complete type (medal or jelly)
            var options = new[]
            {
                new CompletionOption("medal", Loc.GetString("cmd-givecommendation-hint-type-medal")),
                new CompletionOption("jelly", Loc.GetString("cmd-givecommendation-hint-type-jelly"))
            };

            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-givecommendation-hint-type"));
        }

        if (args.Length == 5)
        {
            // Complete award type based on commendation type
            var type = args[3].ToLowerInvariant();

            if (type == "medal" && _medalsDataset != null)
            {
                var count = _medalsDataset.Values.Count;
                var options = Enumerable.Range(1, count)
                    .Select(i =>
                    {
                        var locId = _medalsDataset.Values[i - 1];
                        var name = Loc.GetString(locId);
                        return new CompletionOption(i.ToString(), name);
                    })
                    .ToArray();

                return CompletionResult.FromHintOptions(options,
                    Loc.GetString("cmd-givecommendation-hint-medal-type", ("count", count)));
            }

            if (type == "jelly" && _jelliesDataset != null)
            {
                var count = _jelliesDataset.Values.Count;
                var options = Enumerable.Range(1, count)
                    .Select(i =>
                    {
                        var locId = _jelliesDataset.Values[i - 1];
                        var name = Loc.GetString(locId);
                        return new CompletionOption(i.ToString(), name);
                    })
                    .ToArray();

                return CompletionResult.FromHintOptions(options,
                    Loc.GetString("cmd-givecommendation-hint-jelly-type", ("count", count)));
            }

            return CompletionResult.FromHint(Loc.GetString("cmd-givecommendation-hint-invalid-type"));
        }

        if (args.Length == 6)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-givecommendation-hint-citation"));
        }

        if (args.Length == 7)
        {
            // Show current round number as an option for optional round parameter
            var gameTicker = _systems.GetEntitySystem<GameTicker>();
            var currentRound = gameTicker.RoundId;
            var options = new[]
            {
                new CompletionOption(currentRound.ToString(), Loc.GetString("cmd-givecommendation-hint-round-current"))
            };
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-givecommendation-hint-round"));
        }

        return CompletionResult.Empty;
    }
}
