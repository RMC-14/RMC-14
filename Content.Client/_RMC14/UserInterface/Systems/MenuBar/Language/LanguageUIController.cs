using System.Numerics;
using Content.Client._RMC14.Language.Systems;
using Content.Client.Stylesheets;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.Resources;

namespace Content.Client._RMC14.UserInterface.Systems.Language;

public sealed class LanguageUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private LanguageSystem _languageSystem = default!;
    private MenuButton? _languageButton;
    private LanguageMenuWindow? _window;

    private MenuButton? LanguageButton => _languageButton;

    public void OnStateEntered(GameplayState state)
    {
        _languageSystem = _entitySystemManager.GetEntitySystem<LanguageSystem>();
        _languageSystem.OnLanguagesChanged += OnLanguagesChanged;
        _languageSystem.OnLanguageLearningChanged += OnLanguageLearningChanged;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenLanguageMenu, InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<LanguageUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_languageSystem != null)
        {
            _languageSystem.OnLanguagesChanged -= OnLanguagesChanged;
            _languageSystem.OnLanguageLearningChanged -= OnLanguageLearningChanged;
        }

        _window?.Dispose();
        _window = null;
        UnloadButton();
        CommandBinds.Unregister<LanguageUIController>();
    }

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += LoadButton;
        gameplayStateLoad.OnScreenUnload += UnloadButton;
    }

    private void LoadButton()
    {
        if (_languageButton != null ||
            UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>() is not { } topBar)
        {
            return;
        }

        var button = new MenuButton
        {
            Icon = _resourceCache.GetTexture("/Textures/_RMC14/Interface/flags.rsi/unknown.png"),
            ToolTip = Loc.GetString("game-hud-open-language-menu-button-tooltip"),
            BoundKey = ContentKeyFunctions.OpenLanguageMenu,
            MinSize = new Vector2(42f, 64f),
            HorizontalExpand = true,
        };
        button.AppendStyleClass = StyleBase.ButtonSquare;
        button.OnPressed += LanguageButtonPressed;

        topBar.AddChild(button);
        button.SetPositionInParent(topBar.EmotesButton.GetPositionInParent());
        _languageButton = button;
    }

    private void UnloadButton()
    {
        if (_languageButton == null)
            return;

        _languageButton.OnPressed -= LanguageButtonPressed;
        _languageButton.Dispose();
        _languageButton = null;
    }

    private void LanguageButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    public void ToggleWindow()
    {
        if (_window == null)
        {
            _window = UIManager.CreateWindow<LanguageMenuWindow>();
            _window.OnClose += () =>
            {
                _window = null;
                if (LanguageButton != null)
                    LanguageButton.Pressed = false;
            };
            _window.OnOpen += UpdateLanguageWindow;
            _window.OnLanguageSelected += OnLanguageSelected;
        }

        if (_window.IsOpen)
        {
            _window.Close();
        }
        else
        {
            UpdateLanguageWindow();
            _window.OpenCentered();
        }
    }

    private void OnLanguagesChanged()
    {
        UpdateLanguageWindow();
    }

    private void OnLanguageLearningChanged()
    {
        UpdateLanguageWindow();
    }

    private void UpdateLanguageWindow()
    {
        if (_window == null || _player.LocalSession?.AttachedEntity is not { } entity)
            return;

        var currentLanguage = _languageSystem.GetCurrentLanguage(entity);
        var spokenLanguages = _languageSystem.GetSpokenLanguages(entity);
        var learningLanguages = _languageSystem.GetLearningLanguages(entity);
        _window.UpdateLanguages(currentLanguage, spokenLanguages, learningLanguages);
    }

    private void OnLanguageSelected(ProtoId<LanguagePrototype> language)
    {
        _languageSystem.RequestSetLanguage(language);
    }
}
