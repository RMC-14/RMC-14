using Content.Shared._RMC14.Language;
using Content.Shared.Chat;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Language.Prototypes;

[Prototype]
public sealed partial class LanguagePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = default!;

    [DataField]
    public string? Description;

    [DataField]
    public bool IsVisibleLanguage { get; set; }

    [DataField]
    public Color? TextColor;

    [DataField]
    public string? TypefaceId;

    [DataField]
    public int? TextSize;

    [DataField]
    public string? BoldTypefaceId;

    [DataField]
    public bool ShowLanguageName { get; set; } = false;

    [DataField]
    public string? LanguageIcon;

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
    public int ExpectedVocabularySize { get; set; } = 100;

    [DataField]
    public float DefaultWordComprehension { get; set; } = 0.0f;

    [DataField]
    public float ClearComprehensionThreshold { get; set; } = 0.6f;

    [DataField]
    public float PartialComprehensionThreshold { get; set; } = 0.2f;

    public string LocalizedName => Loc.GetString($"language-{ID}-name");
    public string ChatName => Loc.GetString($"chat-language-{ID}-name");
    public string LocalizedDescription => Loc.GetString($"language-{ID}-description");
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
    public Dictionary<InGameICChatType, LocId> MessageWrapOverrides = new();
}

[Serializable, NetSerializable]
public enum InGameICChatType : byte
{
    Speak,
    Emote,
    Whisper
}
