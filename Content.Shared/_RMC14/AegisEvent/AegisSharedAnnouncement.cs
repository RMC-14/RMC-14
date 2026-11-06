using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Xenonids.Announce;
using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.AegisEvent;

public static class AegisSharedAnnouncement
{
    /// <summary>
    /// Announces the AEGIS event to both Marines and Xenonids
    /// </summary>
    public static void AnnounceToBoth(IEntitySystemManager sysMan, string message)
    {
        var marineAnnounce = sysMan.GetEntitySystem<SharedMarineAnnounceSystem>();
        marineAnnounce.AnnounceHighCommand(Loc.GetString("rmc-aegis-event-announcement-marines"));

        var xenoAnnounce = sysMan.GetEntitySystem<SharedXenoAnnounceSystem>();
        xenoAnnounce.AnnounceAll(default, Loc.GetString("rmc-aegis-event-announcement-xenos"), null);
    }
}


