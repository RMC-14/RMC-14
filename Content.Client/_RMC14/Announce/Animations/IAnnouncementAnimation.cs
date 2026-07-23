namespace Content.Client._RMC14.Announce.Animations;

public interface IAnnouncementAnimation
{
    void Reset(AnnouncementAnimationContext context);
    AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime);
}
