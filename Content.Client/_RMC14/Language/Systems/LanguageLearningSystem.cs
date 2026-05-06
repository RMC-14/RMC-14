using Content.Shared._RMC14.Language.Systems;
using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Language.Prototypes;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Client._RMC14.Language.Systems;

public sealed class LanguageLearningSystem : SharedLanguageLearningSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public LanguageLearningComponent? GetLocalLearner()
    {
        return CompOrNull<LanguageLearningComponent>(_playerManager.LocalEntity);
    }

    public float GetLocalLanguageProgress(ProtoId<LanguagePrototype> language)
    {
        var learner = GetLocalLearner();
        if (learner == null)
            return 0f;
        return learner.LanguageProgress.GetValueOrDefault(language, 0f);
    }

    public Dictionary<string, float> GetLocalLearnedWords(ProtoId<LanguagePrototype> language)
    {
        var learner = GetLocalLearner();
        if (learner?.LearnedWords.TryGetValue(language, out var words) == true)
            return new Dictionary<string, float>(words);
        return new Dictionary<string, float>();
    }

    public HashSet<ProtoId<LanguagePrototype>> GetLocalLearnableLanguages()
    {
        var learner = GetLocalLearner();
        return learner?.LearnableLanguages ?? new HashSet<ProtoId<LanguagePrototype>>();
    }

    public int GetWordCount(ProtoId<LanguagePrototype> language)
    {
        var learner = GetLocalLearner();
        if (learner?.LearnedWords.TryGetValue(language, out var words) == true)
            return words.Count;
        return 0;
    }

    public float GetAverageWordComprehension(ProtoId<LanguagePrototype> language)
    {
        var learner = GetLocalLearner();
        if (learner?.LearnedWords.TryGetValue(language, out var words) == true && words.Count > 0)
            return words.Values.Average();
        return 0f;
    }
}
