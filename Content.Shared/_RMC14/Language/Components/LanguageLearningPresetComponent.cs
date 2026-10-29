using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Language.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Language.Components;

[RegisterComponent]
[Access(typeof(SharedLanguageLearningSystem))]
public sealed partial class LanguageLearningPresetComponent : Component
{
    [DataField]
    public HashSet<ProtoId<LanguagePrototype>> LearnableLanguages = new();

    [DataField]
    public HashSet<ProtoId<LanguagePrototype>> FirstContactLanguages = new();

    [DataField]
    public Dictionary<ProtoId<LanguagePrototype>, LanguageLearningData> Languages = new();

    [DataField]
    public float LearningRange = 8.0f;

    [DataField]
    public float BaseWordLearningRate = 0.45f;

    [DataField]
    public float ComprehensionThreshold = 0.5f;
}
