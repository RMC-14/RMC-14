using Content.Shared._RMC14.Announce.Animations;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class FadeAnimation : IAnnouncementAnimation
{
    private readonly FadeAnimationConfig _config;
    private float _timer;

    public FadeAnimation(FadeAnimationConfig config) => _config = config;

    public void Reset(AnnouncementAnimationContext context)
    {
        _timer = 0f;
        context.Output.FadeAlpha = 0f;
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        _timer += deltaTime;

        var progress = Math.Min(_timer / _config.Duration, 1.0f);
        context.Output.FadeAlpha = progress;

        if (progress >= 1.0f)
        {
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        return AnnouncementAnimationStatus.Running;
    }
}
