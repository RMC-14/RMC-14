using System.Numerics;
using Content.Shared._RMC14.Announce;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Announce;

public sealed class RMCAnnouncementDisplay
{
    public required EntProtoId PresetId;
    public required RMCAnnouncementPresetComponent Preset;
    public required string Text;
    public string? Title;
    public Color Accent;
    public string? SpeakerName;
    public NetEntity? Speaker;
    public EntityUid? Portrait;
    public bool Effects = true;
    public RMCAnnouncementOverride Settings;
    public float Scale = 1;
    public Vector2 Pivot;
    public long Order;
}
