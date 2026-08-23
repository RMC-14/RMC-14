using System.Linq;
using System.Numerics;
using Content.Client._RMC14.Language.Systems;
using Content.Client.Gameplay;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._RMC14.UserInterface.Systems.Language;

public sealed class LanguageUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private LanguageSystem _languageSystem = default!;
    private MenuButton? _languageButton;
    private LanguageMenuWindow? _window;

    private MenuButton? LanguageButton => _languageButton;
    private static readonly SpriteSpecifier.Rsi FallbackLanguageIcon = new(new ResPath("/Textures/_RMC14/Interface/flags.rsi"), "unknown");

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

        _window?.Close();
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
            Icon = GetLanguageIcon(FallbackLanguageIcon),
            ToolTip = Loc.GetString("game-hud-open-language-menu-button-tooltip"),
            BoundKey = ContentKeyFunctions.OpenLanguageMenu,
            MinSize = new Vector2(42f, 64f),
            HorizontalExpand = true,
        };
        button.AppendStyleClass = StyleBase.ButtonSquare;
        button.OnPressed += LanguageButtonPressed;
        if (button.ButtonRoot.Children.FirstOrDefault() is TextureRect icon)
            icon.TextureScale = new Vector2(1.2f, 1.2f);

        topBar.AddChild(button);
        button.SetPositionInParent(topBar.EmotesButton.GetPositionInParent());
        _languageButton = button;
        UpdateLanguageButtonIcon();
    }

    private void UnloadButton()
    {
        if (_languageButton == null)
            return;

        _languageButton.OnPressed -= LanguageButtonPressed;
        _languageButton.Orphan();
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
        UpdateLanguageButtonIcon();
        UpdateLanguageWindow();
    }

    private void OnLanguageLearningChanged()
    {
        UpdateLanguageWindow();
    }

    private void UpdateLanguageButtonIcon()
    {
        if (_languageButton == null ||
            _languageSystem == null ||
            _player.LocalSession?.AttachedEntity is not { } entity)
        {
            return;
        }

        var currentLanguage = _languageSystem.GetCurrentLanguage(entity);
        if (_prototypeManager.TryIndex<LanguagePrototype>(currentLanguage, out var prototype) &&
            GetLanguageIcon(prototype.LanguageIcon) is { } texture)
        {
            _languageButton.Icon = texture;
            return;
        }

        _languageButton.Icon = GetLanguageIcon(FallbackLanguageIcon);
    }

    private Texture? GetLanguageIcon(SpriteSpecifier? icon)
    {
        if (icon == null)
            return null;

        return _entitySystemManager.GetEntitySystem<SpriteSystem>().Frame0(icon);
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
