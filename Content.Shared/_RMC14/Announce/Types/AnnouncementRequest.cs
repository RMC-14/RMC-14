using System;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Announce;

[Flags]
public enum AnnouncementChannels
{
    None = 0,
    Chat = 1 << 0,
    Overlay = 1 << 1,
    Popup = 1 << 2,
    Sound = 1 << 3,
}

public sealed class AnnouncementRequest
{
    public string Message { get; set; } = string.Empty;
    public ProtoId<AnnouncementPresetPrototype>? Preset { get; set; }
    public AnnouncementRoute Route { get; set; } = new();
    public AnnouncementChatOptions? Chat { get; set; }
    public AnnouncementPopupOptions? Popup { get; set; }
    public AnnouncementSoundOptions? Sound { get; set; }
    public float? PriorityOverride { get; set; }
    public bool? CanInterrupt { get; set; }
    public bool? CanBeInterrupted { get; set; }
}

public sealed class AnnouncementRoute
{
    public AnnouncementTarget Target { get; set; } = AnnouncementTarget.All;
    public AnnouncementChannels Channels { get; set; } = AnnouncementChannels.Overlay;
    public EntityUid? Speaker { get; set; }
    public EntityUid? Source { get; set; }
    public string? SpeakerNameOverride { get; set; }
}

public sealed class AnnouncementChatOptions
{
    public string? Message { get; set; }
    public string? WrappedMessage { get; set; }
    public ChatChannel Channel { get; set; } = ChatChannel.Radio;
}

public sealed class AnnouncementPopupOptions
{
    public PopupType Type { get; set; } = PopupType.Small;
    public string? Message { get; set; }
}

public sealed class AnnouncementSoundOptions
{
    public SoundSpecifier? Sound { get; set; }
    public float? Volume { get; set; }
}
