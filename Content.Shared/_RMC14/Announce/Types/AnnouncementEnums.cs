using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Announce;

[Serializable, NetSerializable]
public enum AnnouncementAnimation : byte
{
    Typewriter,
    Slide,
    Zoom,
    Bounce,
    Fade,
    Pulse,
    Heartbeat,
    Warp,
    Glitch,
    None
}

[Serializable, NetSerializable]
public enum AnnouncementPosition : byte
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleCenter,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
    FullScreen
}

[Serializable, NetSerializable]
public enum AnnouncementTarget : byte
{
    All,
    Marines,
    Xenos
}

[Serializable, NetSerializable]
public enum AnnouncementDisplayPreference
{
    Stylized = 0,
    Simplified = 1,
    Disabled = 2,
    Default = 3
}

[Serializable, NetSerializable]
public enum AnnouncementState : byte
{
    Animating,
    Holding,
    FadingOut
}

[Serializable, NetSerializable]
public enum BackgroundType : byte
{
    None,
    Solid,
    Gradient
}

[Serializable, NetSerializable]
public enum GradientDirection : byte
{
    Horizontal,
    Vertical,
    Diagonal,
    Radial
}

[Serializable, NetSerializable]
public enum AnnouncementSpritePosition : byte
{
    Left,
    Right,
    Center,
    Above,
    Below
}

[Serializable, NetSerializable]
public enum AnnouncementSpeakerNamePosition : byte
{
    Above,
    Below,
    Left,
    Right
}

[Serializable, NetSerializable]
public enum AnnouncementTitlePosition : byte
{
    Above,
    Below
}

[Serializable, NetSerializable]
public enum AnnouncementTitleEffectType : byte
{
    None,
    AssaultPulse,
    AssaultScroll
}

[Serializable, NetSerializable]
public enum AnnouncementDecalPlacement : byte
{
    ReplaceSprite,
    BehindSprite,
    Left,
    Right,
    Above,
    Below
}

[Serializable, NetSerializable]
public enum SlideDirection : byte
{
    Top,
    Bottom,
    Left,
    Right
}

[Serializable, NetSerializable]
public enum FrameStyle : byte
{
    Solid,
    Raised,
    Inset,
    Glowing,
    CRT
}

[Serializable, NetSerializable]
public enum SpriteDisplayMode : byte
{
    TopHalf,
    FullSprite,
    HeadOnly,
    CustomClip
}
