using Content.Shared._RMC14.Language.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared._RMC14.Language.Systems;

namespace Content.Shared._RMC14.Language.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedLanguageLearningSystem), typeof(SharedLanguageSystem))]
public sealed partial class LanguageLearningComponent : Component
{
    [DataField]
    public HashSet<ProtoId<LanguagePrototype>> LearnableLanguages = new();

    [DataField]
    public HashSet<ProtoId<LanguagePrototype>> FirstContactLanguages = new();

    [DataField]
    [Access(typeof(SharedLanguageLearningSystem), typeof(SharedLanguageSystem), Other = AccessPermissions.ReadExecute)]
    public Dictionary<ProtoId<LanguagePrototype>, LanguageLearningData> Languages = new();

    [AutoNetworkedField]
    public Dictionary<ProtoId<LanguagePrototype>, LanguageLearningStateData> LanguageStates = new();

    [DataField]
    public Dictionary<NetEntity, int> StudiedSources = new();

    [DataField]
    public int MaxStudiedSourcesTracked = 500;

    [DataField]
    public TimeSpan LastLearningTime = TimeSpan.Zero;

    [DataField]
    public float LearningRange = 8.0f;

    [DataField]
    public float BaseWordLearningRate = 0.45f;

    [DataField]
    public float InitialWordLearningRate = 0.25f;

    [DataField]
    public float FrequencyLearningBonus = 0.05f;

    [DataField]
    public float MaxFrequencyLearningBonus = 0.2f;

    [DataField]
    public float ContextLearningBonus = 0.05f;

    [DataField]
    public float LengthBasedLearningMultiplier = 0.05f;

    [DataField]
    public float ShortWordPenalty = 0.7f;

    [DataField]
    public int MinWordLengthToLearn = 1;

    [DataField]
    public int MaxWordsToLearnPerMessage = 10;

    [DataField]
    public int MaxLearningFromSameSource = 999;

    [DataField]
    public float MinimumDiminishingFactor = 0.2f;

    [DataField]
    public float MinimumDistancePenalty = 0.5f;

    [DataField]
    public TimeSpan MinTimeBetweenLearning = TimeSpan.FromSeconds(2);

    [DataField]
    public float MaxWordComprehension = 0.95f;

    [DataField]
    public float ComprehensionThreshold = 0.5f;

    [DataField]
    public float FluentComprehensionThreshold = 0.6f;

    [DataField]
    public float MasteredComprehensionThreshold = 0.8f;

    [DataField]
    public int MaxContextWordsStored = 50;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class LanguageLearningData
{
    [DataField]
    public bool RequiresFirstContact;

    [DataField]
    public bool Encountered;

    [DataField]
    public bool FluentAnnounced;

    [DataField]
    public float Progress;

    [DataField]
    public int InitialBoostedWordCount;

    [DataField]
    public int BoostedWordsRemaining;

    [DataField]
    public float InitialBoostedWordComprehension;

    [DataField]
    public Dictionary<string, float> LearnedWords = new();

    [DataField]
    public Dictionary<string, int> WordFrequency = new();

    [DataField]
    public Dictionary<string, List<string>> WordContext = new();

    [DataField]
    public Dictionary<string, float> WordPatterns = new();
}

[Serializable, NetSerializable]
public sealed class LanguageLearningStateData
{
    public bool RequiresFirstContact;
    public bool Encountered;
    public float Progress;
    public Dictionary<string, float> LearnedWords = new();

    public LanguageLearningStateData()
    {
    }

    public LanguageLearningStateData(
        bool requiresFirstContact,
        bool encountered,
        float progress,
        Dictionary<string, float> learnedWords)
    {
        RequiresFirstContact = requiresFirstContact;
        Encountered = encountered;
        Progress = progress;
        LearnedWords = learnedWords;
    }
}
