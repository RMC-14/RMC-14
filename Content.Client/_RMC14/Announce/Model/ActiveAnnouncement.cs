using System.Numerics;
using Content.Shared._RMC14.Announce;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Announce;

public sealed class ActiveAnnouncement
{
    public AnnouncementNetData Data { get; set; } = default!;
    public TimeSpan StartTime { get; set; }
    public int CurrentLine { get; set; }
    public int CurrentChar { get; set; }
    public AnnouncementState State { get; set; }

    public string[] CleanText { get; set; } = Array.Empty<string>();

    public Vector2 SlideStartPosition { get; set; }
    public Vector2 CurrentSlideOffset { get; set; }
    public Vector2 CurrentBounceOffset { get; set; }
    public float ZoomCurrentScale { get; set; } = 1.0f;
    public float FadeAlpha { get; set; } = 1.0f;
    public float PulseScale { get; set; } = 1.0f;
    public float PulseAlpha { get; set; } = 1.0f;

    public float TypewriterTimer { get; set; }
    public float GlitchTimer { get; set; }
    public float SlideTimer { get; set; }
    public float ZoomTimer { get; set; }
    public float BounceTimer { get; set; }
    public int BouncePhase { get; set; }
    public float FadeTimer { get; set; }
    public float PulseTimer { get; set; }

    public string[]? GlitchText { get; set; }

    public RichTextLabel[] TitleLabels { get; set; } = Array.Empty<RichTextLabel>();
    public Control? TitleTrack { get; set; }
    public float TitleViewportWidth { get; set; }
    public float TitleContentWidth { get; set; }
    public float TitleScrollGap { get; set; }
    public string TitleText { get; set; } = string.Empty;
    public float TitleRenderedFontSize { get; set; }
}
