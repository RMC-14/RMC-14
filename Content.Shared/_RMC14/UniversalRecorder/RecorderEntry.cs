namespace Content.Shared._RMC14.UniversalRecorder;

public readonly record struct RecorderEntry(
    TimeSpan Timestamp,
    string SpeakerName,
    string SpeechVerb,
    string Text,
    string FontId,
    int FontSize,
    bool Bold,
    string TranscriptLine
);
