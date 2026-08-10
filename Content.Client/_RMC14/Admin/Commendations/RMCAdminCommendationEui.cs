using System.Linq;
using Content.Client._RMC14.Commendations;
using Content.Client.Administration.Systems;
using Content.Client.Eui;
using Content.Shared._RMC14.Admin.Commendations;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Marines.Roles.Ranks;
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
    private RMCAdminCommendationWindow? _window;
    private readonly Dictionary<int, string> _awardNames = new();
    private readonly List<PlayerInfo> _onlinePlayers = new();
    private bool _suppressSuggestions;
    private IEntityManager _entities = default!;
    private SharedRankSystem _rank = default!;

    public override void Opened()
    {
        var prototype = IoCManager.Resolve<IPrototypeManager>();
        var systems = IoCManager.Resolve<IEntitySystemManager>();
        var cs = systems.GetEntitySystem<CommendationSystem>();
        _entities = IoCManager.Resolve<IEntityManager>();
        _rank = systems.GetEntitySystem<SharedRankSystem>();

        var medals = cs.GetAwardableMedalIds().Concat(cs.GetSpecialMedalIds()).ToList();
        var medalItems = medals.Select((id, i) => (Name: prototype.Index<EntityPrototype>(id).Name, Index: i + 1)).ToList();

        var regularJellies = prototype.Index<LocalizedDatasetPrototype>(SharedCommendationSystem.JellyDatasetId).Values;
        var specialJellies = prototype.Index<LocalizedDatasetPrototype>(SharedCommendationSystem.JellySpecialDatasetId).Values;
        var jellyItems = regularJellies.Concat(specialJellies)
            .Select((locId, i) => (Name: Loc.GetString(locId), Index: i + 1))
            .ToList();

        _window = new RMCAdminCommendationWindow();
        _window.CitationEdit.Placeholder = new Rope.Leaf(Loc.GetString("rmc-give-commendation-citation-placeholder"));

        var highCommand = Loc.GetString("rmc-announcement-author-highcommand");
        var queenMother = Loc.GetString("rmc-announcement-author-queen-mother");

        _window.HighCommandButton.OnPressed += _ =>
        {
            _window.GiverEdit.Text = highCommand;
            UpdatePreview();
        };
        _window.QueenMotherButton.OnPressed += _ =>
        {
            _window.GiverEdit.Text = queenMother;
            UpdatePreview();
        };

        _window.MedalButton.OnPressed += args =>
        {
            if (!args.Button.Pressed)
                return;

            _window.JellyButton.Pressed = false;
            PopulateAwards(medalItems);
            UpdatePreview();
        };

        _window.JellyButton.OnPressed += args =>
        {
            if (!args.Button.Pressed)
                return;

            _window.MedalButton.Pressed = false;
            PopulateAwards(jellyItems);
            UpdatePreview();
        };

        _window.GiverEdit.OnTextChanged += _ => UpdatePreview();
        _window.ReceiverNameEdit.OnTextChanged += _ => UpdatePreview();
        _window.AwardOption.OnItemSelected += args =>
        {
            _window.AwardOption.SelectId(args.Id);
            UpdatePreview();
        };

        var admin = systems.GetEntitySystem<AdminSystem>();
        _onlinePlayers.AddRange(admin.PlayerList.Where(p => p.Connected).OrderBy(p => p.Username));
        _window.ReceiverEdit.OnTextChanged += OnReceiverTextChanged;

        _window.SubmitButton.OnPressed += _ => OnSubmit();

        UpdatePreview();
        _window.OpenCentered();
    }

    private void OnReceiverTextChanged(LineEdit.LineEditEventArgs args)
    {
        UpdatePreview();

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

            var button = new Button { Text = label, StyleClasses = { "OpenBoth" } };
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

        UpdatePreview();
    }

    private string GetReceiverName(PlayerInfo player)
    {
        if (player.NetEntity is { } netEntity &&
            _entities.TryGetEntity(netEntity, out var uid))
        {
            var rankName = _rank.GetSpeakerRankName(uid.Value);
            if (!string.IsNullOrWhiteSpace(rankName))
                return rankName;
        }

        return player.CharacterName;
    }

    private void UpdatePreview()
    {
        if (_window == null)
            return;

        var awardName = _window.AwardOption.ItemCount > 0 &&
                        _awardNames.TryGetValue(_window.AwardOption.SelectedId, out var name)
            ? name
            : string.Empty;

        var giver = _window.GiverEdit.Text;
        var receiver = _window.ReceiverNameEdit.Text;

        _window.PreviewTitle.SetMessage(FormattedMessage.FromMarkupOrThrow(
            Loc.GetString("rmc-give-commendation-preview-title", ("name", awardName))));
        _window.PreviewDescription.SetMessage(FormattedMessage.FromMarkupOrThrow(
            Loc.GetString("rmc-give-commendation-preview-description",
                ("receiver", receiver),
                ("giver", giver))));
    }

    private void PopulateAwards(IEnumerable<(string Name, int Index)> items)
    {
        if (_window == null)
            return;

        _window.AwardOption.Clear();
        _awardNames.Clear();
        foreach (var (name, index) in items)
        {
            _window.AwardOption.AddItem(name, index);
            _awardNames[index] = name;
        }
    }

    private void OnSubmit()
    {
        if (_window == null)
            return;

        _window.ErrorLabel.Text = string.Empty;

        if (!_window.MedalButton.Pressed && !_window.JellyButton.Pressed)
        {
            _window.ErrorLabel.Text = Loc.GetString("rmc-give-commendation-error-no-type");
            return;
        }

        if (_window.AwardOption.ItemCount == 0)
        {
            _window.ErrorLabel.Text = Loc.GetString("rmc-give-commendation-error-no-award");
            return;
        }

        var type = _window.MedalButton.Pressed ? CommendationType.Medal : CommendationType.Jelly;
        var awardIndex = _window.AwardOption.SelectedId;

        int? targetRound = null;
        if (!string.IsNullOrWhiteSpace(_window.RoundEdit.Text) &&
            int.TryParse(_window.RoundEdit.Text, out var parsedRound))
        {
            targetRound = parsedRound;
        }

        SendMessage(new RMCAdminGiveCommendationMsg(
            _window.GiverEdit.Text,
            _window.ReceiverEdit.Text,
            _window.ReceiverNameEdit.Text,
            type,
            awardIndex,
            Rope.Collapse(_window.CitationEdit.TextRope),
            targetRound));
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (_window == null)
            return;

        if (msg is RMCAdminGiveCommendationErrorMsg error)
            _window.ErrorLabel.Text = error.Message;
    }

    public override void Closed()
    {
        _window?.Close();
    }
}
