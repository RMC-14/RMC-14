using Content.Shared._RMC14.Language.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Language.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LanguageLearningComponent : Component
{
    [DataField]
    public HashSet<ProtoId<LanguagePrototype>> LearnableLanguages = new();

    [DataField]
    public HashSet<ProtoId<LanguagePrototype>> FirstContactLanguages = new();

    [DataField]
    public Dictionary<ProtoId<LanguagePrototype>, LanguageLearningData> Languages = new();

    [DataField]
    public Dictionary<NetEntity, int> StudiedSources = new();

    [DataField]
    public TimeSpan LastLearningTime = TimeSpan.Zero;

    [DataField]
    public float LearningRange = 8.0f;

    [DataField]
    public float BaseWordLearningRate = 0.45f;

    [DataField]
    public float FrequencyLearningBonus = 0.05f;

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
    public TimeSpan MinTimeBetweenLearning = TimeSpan.FromSeconds(2);

    [DataField]
    public float MaxWordComprehension = 0.95f;

    [DataField]
    public float ComprehensionThreshold = 0.5f;

    [DataField]
    public int MaxContextWordsStored = 50;

    [Serializable, NetSerializable]
    public sealed class State : ComponentState
    {
        public Dictionary<ProtoId<LanguagePrototype>, LanguageLearningStateData> Languages { get; }

        public State(Dictionary<ProtoId<LanguagePrototype>, LanguageLearningStateData> languages)
        {
            Languages = languages;
        }
    }
}

[DataDefinition]
public sealed partial class LanguageLearningData
{
    [DataField]
    public bool RequiresFirstContact;

    [DataField]
    public bool Encountered;

    [DataField]
    public float Progress;

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
    public bool RequiresFirstContact { get; }
    public bool Encountered { get; }
    public float Progress { get; }
    public Dictionary<string, float> LearnedWords { get; }

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
