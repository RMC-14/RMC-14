using System;

namespace Content.Client._RMC14.Announce.Effects;

public interface IAnnouncementVisualEffect
{
    void Apply(AnnouncementEffectContext context, TimeSpan currentTime);
}
