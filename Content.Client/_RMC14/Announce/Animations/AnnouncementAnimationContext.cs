using Content.Shared._RMC14.Announce;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Announce.Animations;

public sealed record AnnouncementAnimationContext(
    ActiveAnnouncement State,
    AnnouncementStyle Style,
    string[] OriginalText,
    string[] CleanText,
    RichTextLabel[] Labels,
    int TitleOffset,
    Func<string, AnnouncementStyle, FormattedMessage> FormatMessage,
    Action SetAllLabels,
    Control? VisualContainer,
    IRobustRandom Random);
