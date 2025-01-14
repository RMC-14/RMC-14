using Content.Client.Credits;
using Content.Client.Lobby;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Systems.Info;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;

namespace Content.Client._RMC14.Roadmap;

public sealed class RoadmapUIController : UIController, IOnStateEntered<LobbyState>
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly InfoUIController _infoUIController = default!;
    [Dependency] private readonly IUriOpener _uriOpener = default!;

    private RoadmapWindow? _window;
    private bool _shown;

    public override void Initialize()
    {
        base.Initialize();
        _infoUIController.Accepted += OnAccepted;
    }

    public void OnStateEntered(LobbyState state)
    {
        if (_shown || _window != null)
            return;

        if (_infoUIController.RulesPopup != null)
            return;

        ToggleRoadmap();
    }

    private void OnAccepted()
    {
        if (!_shown)
            ToggleRoadmap();
    }

    public void ToggleRoadmap()
    {
        if (_window != null)
        {
            _window.Close();
            _window = null;
            return;
        }

        _shown = true;
        _window = new RoadmapWindow();
        _window.OnClose += () => _window = null;

        if (_config.GetCVar(CCVars.InfoLinksDiscord) is { Length: > 0 } discordLink)
        {
            _window.DiscordButton.StyleClasses.Add(StyleBase.ButtonCaution);
            _window.DiscordButton.Visible = true;
            _window.DiscordButton.OnPressed += _ => _uriOpener.OpenUri(discordLink);
        }

        if (_config.GetCVar(CCVars.InfoLinksPatreon) is { Length: > 0 } patreonLink)
        {
            _window.PatreonButton.StyleClasses.Add(StyleBase.ButtonCaution);
            _window.PatreonButton.Visible = true;
            _window.PatreonButton.OnPressed += _ => _uriOpener.OpenUri(patreonLink);
        }

        _window.CreditsButton.StyleClasses.Add(StyleBase.ButtonCaution);
        _window.CreditsButton.OnPressed += _ => new CreditsWindow().OpenCentered();

        _window.OpenCentered();
    }
}
