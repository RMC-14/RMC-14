using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using System.Numerics;

namespace Content.Shared._RMC14.Announce;

public sealed class AnnouncementRequest
{
    public string Message { get; set; } = string.Empty;
    public string? Preset { get; set; }
    public AnnouncementTarget Target { get; set; } = AnnouncementTarget.All;
    public EntityUid? Speaker { get; set; }
    public EntityUid? Source { get; set; }
    public AnnouncementStyleOverride? StyleOverride { get; set; }
    public SoundSpecifier? SoundOverride { get; set; }
    public float? VolumeOverride { get; set; }
    public float? PriorityOverride { get; set; }
    public bool? CanInterrupt { get; set; }
    public bool? CanBeInterrupted { get; set; }
    public bool ShowSprite { get; set; } = true;
    public float SpriteScale { get; set; } = 1.0f;
    public Vector2? SpriteOffset { get; set; }
    public string? SpeakerNameOverride { get; set; }
    public string? Title { get; set; }
    public string? DecalRsi { get; set; }
    public string? DecalState { get; set; }
    public AnnouncementDecalPlacement? DecalPlacement { get; set; }
    public float? DecalScale { get; set; }
    public float? DecalAlpha { get; set; }
    public Vector2? DecalOffset { get; set; }
    public Vector2? TextOffset { get; set; }
}
