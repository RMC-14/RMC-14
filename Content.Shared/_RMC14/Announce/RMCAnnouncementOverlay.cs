using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Announce;

public readonly record struct RMCAnnouncementOverlay(
    EntProtoId<RMCAnnouncementPresetComponent> Preset,
    string Text,
    EntityUid? Speaker = null,
    string? SpeakerName = null,
    string? Title = null,
    Color? Accent = null);
