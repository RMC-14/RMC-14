using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Content.Shared._RMC14.Announce;

[Serializable, NetSerializable]
public sealed class AnnouncementNetData
{
    public string[] Text { get; set; } = Array.Empty<string>();
    public string ConfigId { get; set; } = string.Empty;
    public float Priority { get; set; }
    public bool CanInterrupt { get; set; }
    public bool CanBeInterrupted { get; set; }
    public AnnouncementStyle Style { get; set; } = new();
    public NetEntity? SpeakerEntity { get; set; }
    public string? SpeakerName { get; set; }
    public bool ShowSprite { get; set; } = true;
    public float SpriteScale { get; set; } = 1.0f;
    public Vector2 SpriteOffset { get; set; }
    public Vector2 TextOffset { get; set; }
    public string? Title { get; set; }
    public SoundSpecifier? Sound { get; set; }
    public float SoundVolume { get; set; } = 0f;
    public string? DecalRsi { get; set; }
    public string? DecalState { get; set; }
    public AnnouncementDecalPlacement? DecalPlacement { get; set; }
    public float DecalScale { get; set; } = 4f;
    public float DecalAlpha { get; set; } = 1f;
    public Vector2 DecalOffset { get; set; } = Vector2.Zero;
}

[Serializable, NetSerializable]
public sealed class AnnouncementNetMessage : EntityEventArgs
{
    public AnnouncementNetData Data { get; }

    public AnnouncementNetMessage(AnnouncementNetData data)
    {
        Data = data;
    }
}

[Serializable, NetSerializable]
public sealed class AnnouncementPreferenceNetMessage : EntityEventArgs
{
    public AnnouncementDisplayPreference Preference { get; }
    public Dictionary<string, AnnouncementDisplayPreference> Overrides { get; }

    public AnnouncementPreferenceNetMessage(
        AnnouncementDisplayPreference preference,
        Dictionary<string, AnnouncementDisplayPreference>? overrides = null)
    {
        Preference = preference;
        Overrides = overrides ?? new Dictionary<string, AnnouncementDisplayPreference>();
    }
}
