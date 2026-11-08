using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Announce;

[Serializable, NetSerializable]
public sealed class RMCAnnouncementOverlayMsg(
    EntProtoId preset,
    string text,
    string? title,
    Color? accent,
    string? speakerName,
    string? speakerJob,
    NetEntity? portrait) : EntityEventArgs
{
    public readonly EntProtoId Preset = preset;
    public readonly string Text = text;
    public readonly string? Title = title;
    public readonly Color? Accent = accent;
    public readonly string? SpeakerName = speakerName;
    public readonly string? SpeakerJob = speakerJob;
    public readonly NetEntity? Portrait = portrait;
}
