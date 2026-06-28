using System.Numerics;
using Content.Shared._RMC14.Announce;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Announce;

public sealed class ActiveAnnouncement
{
    public AnnouncementDisplayData Data { get; set; } = default!;
    public AnnouncementStyle ResolvedStyle { get; set; } = new();
    public TimeSpan StartTime { get; set; }
    public AnnouncementState State { get; set; }

    public string[] CleanText { get; set; } = Array.Empty<string>();

    public Vector2 SlideStartPosition { get; set; }
    public Vector2 CurrentSlideOffset { get; set; }
    public Vector2 CurrentBounceOffset { get; set; }
    public float ZoomCurrentScale { get; set; } = 1.0f;
    public float FadeAlpha { get; set; } = 1.0f;
    public float PulseAlpha { get; set; } = 1.0f;

    public RichTextLabel[] TitleLabels { get; set; } = Array.Empty<RichTextLabel>();
    public Control? TitleTrack { get; set; }
    public float TitleViewportWidth { get; set; }
    public float TitleContentWidth { get; set; }
    public float TitleScrollGap { get; set; }
    public string TitleText { get; set; } = string.Empty;
    public float TitleRenderedFontSize { get; set; }
}
