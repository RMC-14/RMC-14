using System.Numerics;
using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.Announce;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Announce;

public sealed class ActiveAnnouncement
{
    public AnnouncementNetData Data { get; set; } = default!;
    public float Priority { get; set; }
    public bool CanInterrupt { get; set; }
    public bool CanBeInterrupted { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan FadeStartTime { get; set; }
    public int CurrentLine { get; set; }
    public int CurrentChar { get; set; }
    public AnnouncementState State { get; set; }
    public string[] CleanText { get; set; } = Array.Empty<string>();
    public RichTextLabel[] RichTextLabels { get; set; } = Array.Empty<RichTextLabel>();
    public RichTextLabel[] GlowLabels { get; set; } = Array.Empty<RichTextLabel>();
    public RichTextLabel[] ShadowLabels { get; set; } = Array.Empty<RichTextLabel>();
    public Control? SpriteContainer { get; set; }
    public Vector2 SlideStartPosition { get; set; }
    public Vector2 OriginalPosition { get; set; }
    public float ZoomCurrentScale { get; set; } = 1.0f;
    public Vector2 CurrentSlideOffset { get; set; }
    public Vector2 CurrentBounceOffset { get; set; }
    public float TypewriterTimer { get; set; }
    public float SlideTimer { get; set; }
    public float ZoomTimer { get; set; }
    public float BounceTimer { get; set; }
    public float GlitchTimer { get; set; }
    public int BouncePhase { get; set; }
    public string[]? GlitchText { get; set; }
    public float FadeAlpha { get; set; } = 1.0f;
    public float PulseScale { get; set; } = 1.0f;
    public float PulseAlpha { get; set; } = 1.0f;
    public float FadeTimer { get; set; }
    public float PulseTimer { get; set; }

    public float FloatTimer { get; set; }
    public float BreatheTimer { get; set; }
    public float ScalePulseTimer { get; set; }
    public float RotationTimer { get; set; }
    public float TintTimer { get; set; }
    public float ShakeTimer { get; set; }
    public float FlashTimer { get; set; }
}
