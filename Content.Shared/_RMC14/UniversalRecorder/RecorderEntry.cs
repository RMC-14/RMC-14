using Content.Shared._RMC14.Language.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.UniversalRecorder;

public readonly record struct RecorderEntry(
    TimeSpan Timestamp,
    string SpeakerName,
    string SpeechVerb,
    string Text,
    string FontId,
    int FontSize,
    bool Bold,
    ProtoId<LanguagePrototype>? Language = null
);
