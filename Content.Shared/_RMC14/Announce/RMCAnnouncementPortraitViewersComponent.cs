using Robust.Shared.Player;

namespace Content.Shared._RMC14.Announce;

[RegisterComponent]
[Access(typeof(RMCAnnouncementOverlaySystem))]
public sealed partial class RMCAnnouncementPortraitViewersComponent : Component
{
    public readonly Dictionary<ICommonSession, TimeSpan> Viewers = new();
}
