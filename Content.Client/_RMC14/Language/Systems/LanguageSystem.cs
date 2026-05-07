using Content.Shared._RMC14.Language;
using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Language.Systems;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Client._RMC14.Language.Systems;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public event Action? OnLanguagesChanged;
    public event Action? OnLanguageLearningChanged;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LanguageComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<LanguageComponent, LanguagesUpdateEvent>(OnLanguagesUpdate);
        SubscribeLocalEvent<LanguageLearningComponent, ComponentHandleState>(OnHandleLearningState);
    }

    private void OnHandleState(Entity<LanguageComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not LanguageComponent.State state)
            return;

        ent.Comp.CurrentLanguage = state.CurrentLanguage;
        ent.Comp.SpokenLanguages = state.SpokenLanguages.ToHashSet();
        ent.Comp.UnderstoodLanguages = state.UnderstoodLanguages.ToHashSet();

        if (ent.Owner == _playerManager.LocalEntity)
            OnLanguagesChanged?.Invoke();
    }

    private void OnHandleLearningState(Entity<LanguageLearningComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not LanguageLearningComponent.State state)
            return;

        ent.Comp.LearnableLanguages = state.LearnableLanguages.ToHashSet();
        ent.Comp.FirstContactLanguages = state.FirstContactLanguages.ToHashSet();
        ent.Comp.EncounteredLanguages = state.EncounteredLanguages.ToHashSet();
        ent.Comp.LanguageProgress = new(state.LanguageProgress);
        ent.Comp.LearnedWords = state.LearnedWords.ToDictionary(
            kvp => kvp.Key,
            kvp => new Dictionary<string, float>(kvp.Value)
        );

        if (ent.Owner == _playerManager.LocalEntity)
            OnLanguageLearningChanged?.Invoke();
    }

    private void OnLanguagesUpdate(Entity<LanguageComponent> ent, ref LanguagesUpdateEvent args)
    {
        if (ent.Owner == _playerManager.LocalEntity)
            OnLanguagesChanged?.Invoke();
    }

    public LanguageComponent? GetLocalSpeaker()
    {
        return CompOrNull<LanguageComponent>(_playerManager.LocalEntity);
    }

    public LanguageLearningComponent? GetLocalLearner()
    {
        return CompOrNull<LanguageLearningComponent>(_playerManager.LocalEntity);
    }

    public void RequestSetLanguage(ProtoId<LanguagePrototype> language)
    {
        if (GetLocalSpeaker()?.CurrentLanguage == language)
            return;

        RaiseNetworkEvent(new LanguagesSetMessage(language));
    }

    public float GetLanguageProgress(EntityUid entity, ProtoId<LanguagePrototype> language)
    {
        return CompOrNull<LanguageLearningComponent>(entity)?.LanguageProgress.GetValueOrDefault(language, 0f) ?? 0f;
    }

    public IReadOnlyDictionary<string, float> GetLearnedWords(EntityUid entity, ProtoId<LanguagePrototype> language)
    {
        var learningComp = CompOrNull<LanguageLearningComponent>(entity);
        if (learningComp?.LearnedWords.TryGetValue(language, out var words) == true)
            return new Dictionary<string, float>(words);

        return new Dictionary<string, float>();
    }

    public IReadOnlySet<ProtoId<LanguagePrototype>> GetLearnableLanguages(EntityUid entity)
    {
        var learningComp = CompOrNull<LanguageLearningComponent>(entity);
        if (learningComp == null)
            return new HashSet<ProtoId<LanguagePrototype>>();

        return new HashSet<ProtoId<LanguagePrototype>>(learningComp.LearnableLanguages);
    }
}
