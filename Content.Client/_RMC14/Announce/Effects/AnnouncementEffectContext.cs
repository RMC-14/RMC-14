using Content.Shared._RMC14.Announce;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Announce.Effects;

public readonly record struct AnnouncementEffectContext(
    AnnouncementStyle Style,
    ActiveAnnouncement State,
    IReadOnlyList<RichTextLabel> Labels,
    bool HasTitle);
