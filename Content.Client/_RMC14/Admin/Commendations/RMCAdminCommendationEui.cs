using System.Linq;
using Content.Client._RMC14.Commendations;
using Content.Client.Administration.Systems;
using Content.Client.Eui;
using Content.Shared._RMC14.Admin.Commendations;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Xenonids.Name;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Dataset;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Admin.Commendations;

[UsedImplicitly]
public sealed class RMCAdminCommendationEui : BaseEui
{
    private readonly record struct AwardEntry(string Id, string Name);

    private RMCAdminCommendationWindow? _window;
    private RMCAdminCommendationPreviewWindow? _previewWindow;
    private readonly List<PlayerInfo> _onlinePlayers = new();
    private bool _suppressSuggestions;
    private CommendationsManager _commendations = default!;
    private IEntityManager _entities = default!;
    private SharedRankSystem _rank = default!;

    public override void Opened()
    {
        var prototype = IoCManager.Resolve<IPrototypeManager>();
        var systems = IoCManager.Resolve<IEntitySystemManager>();
        var cs = systems.GetEntitySystem<CommendationSystem>();
        _commendations = IoCManager.Resolve<CommendationsManager>();
        _entities = IoCManager.Resolve<IEntityManager>();
        _rank = systems.GetEntitySystem<SharedRankSystem>();

        var medalItems = cs.GetAwardableMedalIds()
            .Concat(cs.GetSpecialMedalIds())
            .Select(id => new AwardEntry(id.Id, prototype.Index<EntityPrototype>(id).Name))
            .ToList();

        var regularJellies = prototype.Index<LocalizedDatasetPrototype>(SharedCommendationSystem.JellyDatasetId).Values;
        var specialJellies = prototype.Index<LocalizedDatasetPrototype>(SharedCommendationSystem.JellySpecialDatasetId).Values;
        var jellyItems = regularJellies.Concat(specialJellies)
            .Select(locId => new AwardEntry(locId, Loc.GetString(locId)))
            .ToList();

        _window = new RMCAdminCommendationWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.CitationEdit.Placeholder = new Rope.Leaf(Loc.GetString("rmc-give-commendation-citation-placeholder"));

        var highCommand = Loc.GetString("rmc-announcement-author-highcommand");
        var queenMother = Loc.GetString("rmc-announcement-author-queen-mother");

        _window.HighCommandButton.OnPressed += _ => _window.GiverEdit.Text = highCommand;
        _window.QueenMotherButton.OnPressed += _ => _window.GiverEdit.Text = queenMother;

        _window.MedalButton.OnPressed += args =>
        {
            if (!args.Button.Pressed)
                return;

            _window.JellyButton.Pressed = false;
            PopulateAwards(medalItems);
        };

        _window.JellyButton.OnPressed += args =>
        {
            if (!args.Button.Pressed)
                return;

            _window.MedalButton.Pressed = false;
            PopulateAwards(jellyItems);
        };

        _window.AwardOption.OnItemSelected += args => _window.AwardOption.SelectId(args.Id);

        var admin = systems.GetEntitySystem<AdminSystem>();
        _onlinePlayers.AddRange(admin.PlayerList.Where(p => p.Connected).OrderBy(p => p.Username));
        _window.ReceiverEdit.OnTextChanged += OnReceiverTextChanged;

        _window.PreviewButton.OnPressed += _ => OnPreview();

        _window.OpenCentered();
    }

    private void OnReceiverTextChanged(LineEdit.LineEditEventArgs args)
    {
        if (_window == null || _suppressSuggestions)
            return;

        var suggestions = _window.PlayerSuggestions;
        suggestions.DisposeAllChildren();

        var text = args.Text.Trim();
        if (text.Length == 0)
        {
            suggestions.Visible = false;
            return;
        }

        var matches = _onlinePlayers
            .Where(p => p.Username.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Take(6)
            .ToList();

        if (matches.Count == 0 ||
            (matches.Count == 1 && matches[0].Username.Equals(text, StringComparison.OrdinalIgnoreCase)))
        {
            suggestions.Visible = false;
            return;
        }

        foreach (var player in matches)
        {
            var label = string.IsNullOrWhiteSpace(player.CharacterName)
                ? player.Username
                : $"{player.Username} ({player.CharacterName})";

            var button = new Button { Text = label, StyleClasses = { "OpenBoth", CMStyleClasses.RMCButtonDarkGold } };
            button.OnPressed += _ => SelectPlayer(player);
            suggestions.AddChild(button);
        }

        suggestions.Visible = true;
    }

    private void SelectPlayer(PlayerInfo player)
    {
        if (_window == null)
            return;

        _suppressSuggestions = true;
        _window.ReceiverEdit.Text = player.Username;
        var receiverName = GetReceiverName(player);
        if (!string.IsNullOrWhiteSpace(receiverName))
            _window.ReceiverNameEdit.Text = receiverName;
        _suppressSuggestions = false;

        _window.PlayerSuggestions.DisposeAllChildren();
        _window.PlayerSuggestions.Visible = false;
    }

    private string GetReceiverName(PlayerInfo player)
    {
        if (player.NetEntity is { } netEntity &&
            _entities.TryGetEntity(netEntity, out var uid))
        {
            if (_entities.HasComponent<XenoNameComponent>(uid.Value))
            {
                var xenoName = _entities.GetComponent<MetaDataComponent>(uid.Value).EntityName;
                if (!string.IsNullOrWhiteSpace(xenoName))
                    return xenoName;
            }
            else
            {
                var rankName = _rank.GetSpeakerRankName(uid.Value);
                if (!string.IsNullOrWhiteSpace(rankName))
                    return rankName;
            }
        }

        return player.CharacterName;
    }

    private void PopulateAwards(IEnumerable<AwardEntry> items)
    {
        if (_window == null)
            return;

        _window.AwardOption.Clear();
        foreach (var award in items)
        {
            _window.AwardOption.AddItem(award.Name);
            _window.AwardOption.SetItemMetadata(_window.AwardOption.ItemCount - 1, award);
        }
    }

    private void OnPreview()
    {
        if (_window == null)
            return;

        _window.ErrorLabel.Text = string.Empty;

        if (!_window.MedalButton.Pressed && !_window.JellyButton.Pressed)
        {
            _window.ErrorLabel.Text = Loc.GetString("rmc-give-commendation-error-no-type");
            return;
        }

        if (_window.AwardOption.ItemCount == 0 ||
            _window.AwardOption.SelectedMetadata is not AwardEntry award)
        {
            _window.ErrorLabel.Text = Loc.GetString("rmc-give-commendation-error-no-award");
            return;
        }

        var type = _window.MedalButton.Pressed ? CommendationType.Medal : CommendationType.Jelly;

        int? targetRound = null;
        var roundText = _window.RoundEdit.Text.Trim();
        if (roundText.Length > 0)
        {
            if (!int.TryParse(roundText, out var parsedRound))
            {
                _window.ErrorLabel.Text = Loc.GetString("rmc-give-commendation-error-invalid-round");
                return;
            }

            targetRound = parsedRound;
        }

        var giver = _window.GiverEdit.Text;
        var receiverName = _window.ReceiverNameEdit.Text;
        var citation = Rope.Collapse(_window.CitationEdit.TextRope);

        var message = new RMCAdminGiveCommendationMsg(
            giver,
            _window.ReceiverEdit.Text,
            receiverName,
            type,
            award.Id,
            citation,
            targetRound);

        var commendation = new Commendation(giver, receiverName, award.Name, citation.Trim(), type, targetRound ?? 0);
        OpenPreview(message, commendation);
    }

    private void OpenPreview(RMCAdminGiveCommendationMsg message, Commendation commendation)
    {
        _previewWindow?.Close();

        var container = _commendations.CreateCommendationContainer(commendation);
        if (message.TargetRound == null)
        {
            container.Title.Text = Loc.GetString("rmc-commendation-title",
                ("round", Loc.GetString("rmc-give-commendation-preview-current-round")),
                ("name", commendation.Name));
        }

        var preview = new RMCAdminCommendationPreviewWindow();
        preview.PreviewContainer.AddChild(container);
        preview.CancelButton.OnPressed += _ => preview.Close();
        preview.ConfirmButton.OnPressed += _ =>
        {
            if (_window != null)
                _window.PreviewButton.Disabled = true;

            SendMessage(message);
            preview.Close();
        };
        preview.OnClose += () => _previewWindow = null;

        _previewWindow = preview;
        preview.OpenCentered();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (_window == null)
            return;

        if (msg is not RMCAdminGiveCommendationErrorMsg error)
            return;

        _window.ErrorLabel.Text = error.Message;
        _window.PreviewButton.Disabled = false;
    }

    public override void Closed()
    {
        _previewWindow?.Close();
        _window?.Close();
    }
}
