using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Dataset;
using Content.Shared.Mind;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Xenonids.Name;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Content.Server._RMC14.Commendations;

namespace Content.Server._RMC14.Admin.Commendations;

[AdminCommand(AdminFlags.Commendations)]
public sealed class RMCGiveCommendationCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    private LocalizedDatasetPrototype? _jelliesDataset;
    private LocalizedDatasetPrototype? _jelliesSpecialDataset;
    private IReadOnlyList<EntProtoId>? _medalIds;
    private IReadOnlyList<EntProtoId>? _specialMedalIds;

    public override string Command => "rmcgivecommendation";

    private int MedalCount => (_medalIds?.Count ?? 0) + (_specialMedalIds?.Count ?? 0);
    private int JellyCount => (_jelliesDataset?.Values.Count ?? 0) + (_jelliesSpecialDataset?.Values.Count ?? 0);

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var cs = _systems.GetEntitySystem<CommendationSystem>();
        _medalIds ??= cs.GetAwardableMedalIds();
        _specialMedalIds ??= cs.GetSpecialMedalIds();
        _jelliesDataset ??= _prototype.Index<LocalizedDatasetPrototype>(SharedCommendationSystem.JellyDatasetId);
        _jelliesSpecialDataset ??= _prototype.Index<LocalizedDatasetPrototype>(SharedCommendationSystem.JellySpecialDatasetId);

        if (args.Length < 6)
        {
            shell.WriteError(Loc.GetString("cmd-rmcgivecommendation-invalid-arguments"));
            shell.WriteLine(Help);
            return;
        }

        var giverName = args[0];
        var receiverNameOrId = args[1];
        var receiverName = args[2];
        var commendationTypeStr = args[3].ToLowerInvariant();
        var awardTypeStr = args[4];
        var citation = args[5];

        int? targetRound = null;
        if (args.Length == 7 && int.TryParse(args[6], out var parsedRound))
            targetRound = parsedRound;

        CommendationType commendationType;
        switch (commendationTypeStr)
        {
            case "medal":
                commendationType = CommendationType.Medal;
                break;
            case "jelly":
                commendationType = CommendationType.Jelly;
                break;
            default:
                shell.WriteError(Loc.GetString("cmd-rmcgivecommendation-invalid-type"));
                shell.WriteLine(Help);
                return;
        }

        if (!int.TryParse(awardTypeStr, out var awardIndex) ||
            !cs.TryGetAwardInfo(commendationType, awardIndex, out var awardName, out var protoId))
        {
            var max = commendationType == CommendationType.Medal ? MedalCount : JellyCount;
            shell.WriteError(Loc.GetString("cmd-rmcgivecommendation-invalid-award-type",
                ("type", commendationTypeStr), ("max", max)));
            shell.WriteLine(Help);
            return;
        }

        var adminId = shell.Player?.UserId.UserId ?? Guid.Empty;
        var adminName = shell.Player?.Name ?? "Server";

        var error = await cs.AdminGiveCommendation(
            adminId,
            adminName,
            giverName,
            receiverNameOrId,
            receiverName,
            commendationType,
            awardName,
            protoId,
            citation,
            targetRound);

        if (error != null)
            shell.WriteError(error);
        else
            shell.WriteLine(Loc.GetString("cmd-rmcgivecommendation-success", ("award", awardName), ("player", receiverNameOrId)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var commendationSystem = _systems.GetEntitySystem<CommendationSystem>();
        _medalIds ??= commendationSystem.GetAwardableMedalIds();
        _specialMedalIds ??= commendationSystem.GetSpecialMedalIds();
        _jelliesDataset ??= _prototype.Index<LocalizedDatasetPrototype>(SharedCommendationSystem.JellyDatasetId);
        _jelliesSpecialDataset ??= _prototype.Index<LocalizedDatasetPrototype>(SharedCommendationSystem.JellySpecialDatasetId);

        if (args.Length == 1)
        {
            var highCommandName = Loc.GetString("rmc-announcement-author-highcommand");
            var queenMotherName = Loc.GetString("rmc-announcement-author-queen-mother");

            var options = new[]
            {
                new CompletionOption(highCommandName, Loc.GetString("cmd-rmcgivecommendation-hint-giver-highcommand")),
                new CompletionOption(queenMotherName, Loc.GetString("cmd-rmcgivecommendation-hint-giver-queen-mother"))
            };
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-rmcgivecommendation-hint-giver"));
        }

        if (args.Length == 2)
        {
            var options = _players.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-rmcgivecommendation-hint-receiver"));
        }

        if (args.Length == 3)
        {
            var receiverNameOrId = args[1];
            var mindSystem = _systems.GetEntitySystem<SharedMindSystem>();
            var characterNames = new List<CompletionOption>();

            foreach (var session in _players.Sessions)
            {
                if (!session.Name.Equals(receiverNameOrId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (mindSystem.TryGetMind(session, out _, out var mind) && !string.IsNullOrWhiteSpace(mind.CharacterName))
                {
                    var completionName = GetCompletionCharacterName(mind.CurrentEntity, mind.CharacterName);
                    characterNames.Add(new CompletionOption(completionName, $"{session.Name} as {mind.CharacterName}"));
                }
            }

            if (characterNames.Count > 0)
                return CompletionResult.FromHintOptions(characterNames, Loc.GetString("cmd-rmcgivecommendation-hint-receiver-name"));

            return CompletionResult.FromHint(Loc.GetString("cmd-rmcgivecommendation-hint-receiver-name"));
        }

        if (args.Length == 4)
        {
            var options = new[]
            {
                new CompletionOption("medal", Loc.GetString("cmd-rmcgivecommendation-hint-type-medal")),
                new CompletionOption("jelly", Loc.GetString("cmd-rmcgivecommendation-hint-type-jelly"))
            };

            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-rmcgivecommendation-hint-type"));
        }

        if (args.Length == 5)
        {
            var type = args[3].ToLowerInvariant();

            if (type == "medal")
            {
                var options = GetMedalCompletionOptions();
                return CompletionResult.FromHintOptions(options,
                    Loc.GetString("cmd-rmcgivecommendation-hint-medal-type", ("count", MedalCount)));
            }

            if (type == "jelly")
            {
                var options = GetJellyCompletionOptions();
                return CompletionResult.FromHintOptions(options,
                    Loc.GetString("cmd-rmcgivecommendation-hint-jelly-type", ("count", JellyCount)));
            }

            return CompletionResult.FromHint(Loc.GetString("cmd-rmcgivecommendation-hint-invalid-type"));
        }

        if (args.Length == 6)
            return CompletionResult.FromHint(Loc.GetString("cmd-rmcgivecommendation-hint-citation"));

        if (args.Length == 7)
        {
            var gameTicker = _systems.GetEntitySystem<GameTicker>();
            var currentRound = gameTicker.RoundId;
            var options = new[]
            {
                new CompletionOption(currentRound.ToString(), Loc.GetString("cmd-rmcgivecommendation-hint-round-current"))
            };
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-rmcgivecommendation-hint-round"));
        }

        return CompletionResult.Empty;
    }

    private string GetCompletionCharacterName(EntityUid? entity, string characterName)
    {
        if (entity == null)
            return characterName;

        if (_entities.HasComponent<XenoNameComponent>(entity.Value))
            return characterName;

        var rankSystem = _systems.GetEntitySystem<SharedRankSystem>();
        return rankSystem.GetSpeakerFullRankName(entity.Value) ?? characterName;
    }

    private CompletionOption[] GetJellyCompletionOptions()
    {
        var options = new List<CompletionOption>();
        var regularCount = _jelliesDataset?.Values.Count ?? 0;

        if (_jelliesDataset != null)
        {
            for (var i = 1; i <= regularCount; i++)
                options.Add(new CompletionOption(i.ToString(), Loc.GetString(_jelliesDataset.Values[i - 1])));
        }

        if (_jelliesSpecialDataset != null)
        {
            for (var i = 1; i <= _jelliesSpecialDataset.Values.Count; i++)
                options.Add(new CompletionOption((regularCount + i).ToString(), Loc.GetString(_jelliesSpecialDataset.Values[i - 1])));
        }

        return options.ToArray();
    }

    private CompletionOption[] GetMedalCompletionOptions()
    {
        var options = new List<CompletionOption>();
        var medalIds = _medalIds ?? Array.Empty<EntProtoId>();
        var specialMedalIds = _specialMedalIds ?? Array.Empty<EntProtoId>();

        for (var i = 0; i < medalIds.Count; i++)
        {
            var proto = _prototype.Index<EntityPrototype>(medalIds[i]);
            options.Add(new CompletionOption((i + 1).ToString(), proto.Name));
        }

        var offset = medalIds.Count;
        for (var i = 0; i < specialMedalIds.Count; i++)
        {
            var proto = _prototype.Index<EntityPrototype>(specialMedalIds[i]);
            options.Add(new CompletionOption((offset + i + 1).ToString(), proto.Name));
        }

        return options.ToArray();
    }
}