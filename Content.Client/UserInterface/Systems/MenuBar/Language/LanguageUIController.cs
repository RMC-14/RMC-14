using Content.Client._RMC14.Language.Systems;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Language;

public sealed class LanguageUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private LanguageSystem _languageSystem = default!;
    private LanguageMenuWindow? _window;

    private MenuButton? LanguageButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.LanguageButton;

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
        CommandBinds.Unregister<LanguageUIController>();
    }

    private void OnLocalPlayerChanged(EntityUid? oldEntity, EntityUid? newEntity)
    {
        UpdateLanguageWindow();
    }

    public void LoadButton()
    {
        if (UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>() is not { } gameTopMenuBar)
            return;

        if (gameTopMenuBar.LanguageButton == null)
            return;

        gameTopMenuBar.LanguageButton.OnPressed += LanguageButtonPressed;
    }

    public void UnloadButton()
    {
        if (UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>() is not { } gameTopMenuBar)
            return;

        if (gameTopMenuBar.LanguageButton == null)
            return;

        gameTopMenuBar.LanguageButton.OnPressed -= LanguageButtonPressed;
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
