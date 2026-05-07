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
        return learner.Languages.GetValueOrDefault(language)?.Progress ?? 0f;
    }

    public Dictionary<string, float> GetLocalLearnedWords(ProtoId<LanguagePrototype> language)
    {
        var learner = GetLocalLearner();
        if (learner?.Languages.TryGetValue(language, out var languageData) == true)
            return new Dictionary<string, float>(languageData.LearnedWords);
        return new Dictionary<string, float>();
    }

    public HashSet<ProtoId<LanguagePrototype>> GetLocalLearnableLanguages()
    {
        var learner = GetLocalLearner();
        return learner == null
            ? new HashSet<ProtoId<LanguagePrototype>>()
            : learner.Languages.Keys.ToHashSet();
    }

    public int GetWordCount(ProtoId<LanguagePrototype> language)
    {
        var learner = GetLocalLearner();
        if (learner?.Languages.TryGetValue(language, out var languageData) == true)
            return languageData.LearnedWords.Count;
        return 0;
    }

    public float GetAverageWordComprehension(ProtoId<LanguagePrototype> language)
    {
        var learner = GetLocalLearner();
        if (learner?.Languages.TryGetValue(language, out var languageData) == true &&
            languageData.LearnedWords.Count > 0)
        {
            return languageData.LearnedWords.Values.Average();
        }
        return 0f;
    }
}
