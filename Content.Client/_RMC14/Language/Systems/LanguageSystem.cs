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
    [Dependency] private readonly IPlayerManager _player = default!;

    public event Action? OnLanguagesChanged;
    public event Action? OnLanguageLearningChanged;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LanguageComponent, AfterAutoHandleStateEvent>(OnLanguageAfterState);
        SubscribeLocalEvent<LanguageLearningComponent, ComponentHandleState>(OnHandleLearningState);
    }

    private void OnLanguageAfterState(Entity<LanguageComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Owner == _player.LocalEntity)
            OnLanguagesChanged?.Invoke();
    }

    private void OnHandleLearningState(Entity<LanguageLearningComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not LanguageLearningComponent.State state)
            return;

        ent.Comp.LearnableLanguages = state.Languages.Keys.ToHashSet();
        ent.Comp.FirstContactLanguages = state.Languages
            .Where(kvp => kvp.Value.RequiresFirstContact)
            .Select(kvp => kvp.Key)
            .ToHashSet();
        ent.Comp.Languages = state.Languages.ToDictionary(
            kvp => kvp.Key,
            kvp => new LanguageLearningData
            {
                RequiresFirstContact = kvp.Value.RequiresFirstContact,
                Encountered = kvp.Value.Encountered,
                Progress = kvp.Value.Progress,
                LearnedWords = new Dictionary<string, float>(kvp.Value.LearnedWords),
            }
        );

        if (ent.Owner == _player.LocalEntity)
            OnLanguageLearningChanged?.Invoke();
    }

    public LanguageComponent? GetLocalSpeaker()
    {
        return CompOrNull<LanguageComponent>(_player.LocalEntity);
    }

    public LanguageLearningComponent? GetLocalLearner()
    {
        return CompOrNull<LanguageLearningComponent>(_player.LocalEntity);
    }

    public void RequestSetLanguage(ProtoId<LanguagePrototype> language)
    {
        if (GetLocalSpeaker()?.CurrentLanguage == language)
            return;

        RaiseNetworkEvent(new LanguagesSetMessage(language));
    }

    public float GetLanguageProgress(EntityUid entity, ProtoId<LanguagePrototype> language)
    {
        var learningComp = CompOrNull<LanguageLearningComponent>(entity);
        return learningComp?.Languages.GetValueOrDefault(language)?.Progress ?? 0f;
    }

    public IReadOnlyDictionary<string, float> GetLearnedWords(EntityUid entity, ProtoId<LanguagePrototype> language)
    {
        var learningComp = CompOrNull<LanguageLearningComponent>(entity);
        if (learningComp?.Languages.TryGetValue(language, out var languageData) == true)
            return new Dictionary<string, float>(languageData.LearnedWords);

        return new Dictionary<string, float>();
    }

    public IReadOnlySet<ProtoId<LanguagePrototype>> GetLearnableLanguages(EntityUid entity)
    {
        var learningComp = CompOrNull<LanguageLearningComponent>(entity);
        if (learningComp == null)
            return new HashSet<ProtoId<LanguagePrototype>>();

        return new HashSet<ProtoId<LanguagePrototype>>(learningComp.Languages.Keys);
    }
}
