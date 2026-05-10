using Content.Shared._RMC14.Language;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Language.Prototypes;

[Prototype]
public sealed partial class LanguagePrototype : IPrototype
{
    public const float DefaultWordComprehensionValue = 0.0f;
    public const float DefaultClearComprehensionThresholdValue = 0.6f;
    public const float DefaultPartialComprehensionThresholdValue = 0.2f;
    public const string DefaultPartialGarbleCharactersValue = "~?*#";
    public const float DefaultMinimumPartialGarbleRateValue = 0.1f;
    public const float DefaultPartialGarbleRateMultiplierValue = 0.75f;
    public const int DefaultMinimumRequiredLearnedWordsValue = 15;
    public const float DefaultMaximumOverallComprehensionValue = 0.95f;
    public const float DefaultLearnedWordComprehensionWeightValue = 0.7f;
    public const float DefaultVocabularyCompletenessWeightValue = 0.3f;
    public const float DefaultLearningRateMultiplierValue = 1.0f;

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = default!;

    [DataField]
    public string? Description;

    [DataField]
    public bool IsVisibleLanguage { get; set; }

    [DataField]
    public string? TypefaceId;

    [DataField]
    public int? TextSize;

    [DataField]
    public string? BoldTypefaceId;

    [DataField]
    public bool ShowLanguageName { get; set; } = false;

    [DataField]
    public bool ShowLanguageIcon { get; set; } = true;

    [DataField]
    public SpriteSpecifier? LanguageIcon;

    [DataField]
    public bool CanUseRadio { get; set; } = true;

    [DataField]
    public bool NeedsSpeech { get; set; } = true;

    [DataField]
    public bool NeedsLOS { get; set; } = false;

    [DataField]
    public ObfuscationMethod ObfuscationMethod = ObfuscationMethod.Default;

    [DataField]
    public bool RandomizeObfuscation { get; set; }

    [DataField]
    public SpeechOverrideInfo SpeechOverride = new();

    [DataField]
    public LocId? FirstContactMeaning;

    [DataField]
    public int ExpectedVocabularySize { get; set; } = 100;

    [DataField]
    public float DefaultWordComprehension { get; set; } = DefaultWordComprehensionValue;

    [DataField]
    public float ClearComprehensionThreshold { get; set; } = DefaultClearComprehensionThresholdValue;

    [DataField]
    public float PartialComprehensionThreshold { get; set; } = DefaultPartialComprehensionThresholdValue;

    [DataField]
    public string PartialGarbleCharacters { get; set; } = DefaultPartialGarbleCharactersValue;

    [DataField]
    public float MinimumPartialGarbleRate { get; set; } = DefaultMinimumPartialGarbleRateValue;

    [DataField]
    public float PartialGarbleRateMultiplier { get; set; } = DefaultPartialGarbleRateMultiplierValue;

    [DataField]
    public int MinimumRequiredLearnedWords { get; set; } = DefaultMinimumRequiredLearnedWordsValue;

    [DataField]
    public float MaximumOverallComprehension { get; set; } = DefaultMaximumOverallComprehensionValue;

    [DataField]
    public float LearnedWordComprehensionWeight { get; set; } = DefaultLearnedWordComprehensionWeightValue;

    [DataField]
    public float VocabularyCompletenessWeight { get; set; } = DefaultVocabularyCompletenessWeightValue;

    [DataField]
    public float LearningRateMultiplier { get; set; } = DefaultLearningRateMultiplierValue;

    public string LocalizedName => Loc.GetString($"language-{ID}-name");
    public string ChatName => Loc.GetString($"chat-language-{ID}-name");
    public string LocalizedDescription => Loc.GetString($"language-{ID}-description");
    public string? DisplayedLanguageIcon => ShowLanguageIcon ? ID : null;
}

[DataDefinition]
public sealed partial class SpeechOverrideInfo
{
    [DataField]
    public Color? Color = null;

    [DataField]
    public string? FontId;

    [DataField]
    public int? FontSize;

    [DataField]
    public string? BoldFontId;

    [DataField]
    public bool AllowRadio = true;

    [DataField]
    public bool RequireSpeech = true;

    [DataField]
    public bool RequireLOS = false;

    [DataField]
    public InGameICChatType? ChatTypeOverride;

    [DataField]
    public List<LocId>? SpeechVerbOverrides;

    [DataField]
    public ProtoId<SpeechSoundsPrototype>? SpeechSoundsOverride;

    [DataField]
    public Dictionary<InGameICChatType, LocId> MessageWrapOverrides = new();
}

[Serializable, NetSerializable]
public enum InGameICChatType : byte
{
    Speak,
    Emote,
    Whisper
}
