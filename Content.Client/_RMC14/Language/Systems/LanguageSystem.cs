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
    private static readonly IReadOnlyDictionary<ProtoId<LanguagePrototype>, LanguageLearningViewData> EmptyLearningLanguages =
        new Dictionary<ProtoId<LanguagePrototype>, LanguageLearningViewData>();

    public event Action? OnLanguagesChanged;
    public event Action? OnLanguageLearningChanged;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LanguageComponent, AfterAutoHandleStateEvent>(OnLanguageAfterState);
        SubscribeLocalEvent<LanguageLearningComponent, AfterAutoHandleStateEvent>(OnLearningAfterState);
    }

    private void OnLanguageAfterState(Entity<LanguageComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Owner == _player.LocalEntity)
            OnLanguagesChanged?.Invoke();
    }

    private void OnLearningAfterState(Entity<LanguageLearningComponent> ent, ref AfterAutoHandleStateEvent args)
    {
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

    public IReadOnlyDictionary<ProtoId<LanguagePrototype>, LanguageLearningViewData> GetLearningLanguages(EntityUid entity)
    {
        if (CompOrNull<LanguageLearningComponent>(entity) is not { } learningComp)
            return EmptyLearningLanguages;

        return learningComp.LanguageStates.ToDictionary(
            kvp => kvp.Key,
            kvp => new LanguageLearningViewData(
                kvp.Value.RequiresFirstContact,
                kvp.Value.Encountered,
                kvp.Value.Progress,
                new Dictionary<string, float>(kvp.Value.LearnedWords)));
    }
}
