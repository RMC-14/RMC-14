namespace Content.Shared._RMC14.Announce;

public enum RMCAnnouncementTemplate : byte
{
    Banner,
    Portrait,
    TitleCard,
}

public enum RMCAnnouncementPosition : byte
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
}

public enum RMCAnnouncementDisplayMode
{
    Full,
    Simple,
    Off,
}
