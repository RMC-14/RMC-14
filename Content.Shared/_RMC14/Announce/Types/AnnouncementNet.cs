using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared._RMC14.Announce;

[Serializable, NetSerializable]
public sealed class AnnouncementNetData
{
    public string[] Text { get; set; } = Array.Empty<string>();
    public ProtoId<AnnouncementPresetPrototype> AnnouncementId { get; set; } = string.Empty;
    public float Priority { get; set; }
    public bool CanInterrupt { get; set; }
    public bool CanBeInterrupted { get; set; }
    public NetEntity? SpeakerEntity { get; set; }
    public string? SpeakerName { get; set; }
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

