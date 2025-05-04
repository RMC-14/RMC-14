﻿using Content.Client._RMC14.LinkAccount;
using Content.Client._RMC14.Roadmap;
using Content.Client.Credits;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Guidebook;
using Content.Client.UserInterface.Systems.Info;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.EscapeMenu;

[UsedImplicitly]
public sealed class EscapeUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IClientConsoleHost _console = default!;
    [Dependency] private readonly IUriOpener _uri = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ChangelogUIController _changelog = default!;
    [Dependency] private readonly InfoUIController _info = default!;
    [Dependency] private readonly OptionsUIController _options = default!;
    [Dependency] private readonly GuidebookUIController _guidebook = default!;
    [Dependency] private readonly LinkAccountManager _linkAccount = default!;

    private Options.UI.EscapeMenu? _escapeWindow;

    private MenuButton? EscapeButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.EscapeButton;

    public override void Initialize()
    {
        _linkAccount.Updated += () =>
        {
            if (_escapeWindow != null)
                _escapeWindow.PatronPerksButton.Visible = _linkAccount.CanViewPatronPerks();
        };
    }

    public void UnloadButton()
    {
        if (EscapeButton == null)
        {
            return;
        }

        EscapeButton.Pressed = false;
        EscapeButton.OnPressed -= EscapeButtonOnOnPressed;
    }

    public void LoadButton()
    {
        if (EscapeButton == null)
        {
            return;
        }

        EscapeButton.OnPressed += EscapeButtonOnOnPressed;
    }

    private void ActivateButton() => EscapeButton!.SetClickPressed(true);
    private void DeactivateButton() => EscapeButton!.SetClickPressed(false);

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_escapeWindow == null);

        _escapeWindow = UIManager.CreateWindow<Options.UI.EscapeMenu>();

        _escapeWindow.OnClose += DeactivateButton;
        _escapeWindow.OnOpen += ActivateButton;

        _escapeWindow.ChangelogButton.OnPressed += _ =>
        {
            CloseEscapeWindow();
            _changelog.ToggleWindow();
        };

        _escapeWindow.CreditsButton.OnPressed += _ => new CreditsWindow().OpenCentered();

        _escapeWindow.PatronPerksButton.Visible = _linkAccount.CanViewPatronPerks();
        _escapeWindow.PatronPerksButton.OnPressed += _ =>
        {
            CloseEscapeWindow();
            UIManager.GetUIController<LinkAccountUIController>().TogglePatronPerksWindow();
        };

        _escapeWindow.RoadmapButton.OnPressed += _ =>
        {
            CloseEscapeWindow();
            UIManager.GetUIController<RoadmapUIController>().ToggleRoadmap();
        };

        _escapeWindow.RulesButton.OnPressed += _ =>
        {
            CloseEscapeWindow();
            _info.OpenWindow();
        };

        _escapeWindow.DisconnectButton.OnPressed += _ =>
        {
            CloseEscapeWindow();
            _console.ExecuteCommand("disconnect");
        };

        _escapeWindow.OptionsButton.OnPressed += _ =>
        {
            CloseEscapeWindow();
            _options.OpenWindow();
        };

        _escapeWindow.QuitButton.OnPressed += _ =>
        {
            CloseEscapeWindow();
            _console.ExecuteCommand("quit");
        };

        _escapeWindow.WikiButton.OnPressed += _ =>
        {
            _uri.OpenUri(_cfg.GetCVar(CCVars.InfoLinksWiki));
        };

        _escapeWindow.GuidebookButton.OnPressed += _ =>
        {
            _guidebook.ToggleGuidebook();
        };

        // Hide wiki button if we don't have a link for it.
        _escapeWindow.WikiButton.Visible = _cfg.GetCVar(CCVars.InfoLinksWiki) != "";

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.EscapeMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<EscapeUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_escapeWindow != null)
        {
            _escapeWindow.Dispose();
            _escapeWindow = null;
        }

        CommandBinds.Unregister<EscapeUIController>();
    }

    private void EscapeButtonOnOnPressed(ButtonEventArgs obj)
    {
        ToggleWindow();
    }

    private void CloseEscapeWindow()
    {
        _escapeWindow?.Close();
    }

    /// <summary>
    /// Toggles the game menu.
    /// </summary>
    public void ToggleWindow()
    {
        if (_escapeWindow == null)
            return;

        if (_escapeWindow.IsOpen)
        {
            CloseEscapeWindow();
            EscapeButton!.Pressed = false;
        }
        else
        {
            _escapeWindow.OpenCentered();
            EscapeButton!.Pressed = true;
        }
    }
}
