namespace Content.Client._RMC14.Announce.Animations;

public interface IAnnouncementAnimation
{
    void Reset(AnnouncementAnimationContext context);
    bool Update(AnnouncementAnimationContext context, float deltaTime);
}
