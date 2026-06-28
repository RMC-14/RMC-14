using System.Numerics;
using Content.Shared._RMC14.Announce.Animations;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class BounceAnimation : IAnnouncementAnimation
{
    private readonly BounceAnimationConfig _config;
    private float _timer;
    private int _phase;

    public BounceAnimation(BounceAnimationConfig config) => _config = config;

    public void Reset(AnnouncementAnimationContext context)
    {
        _timer = 0f;
        _phase = 0;
        context.Output.CurrentBounceOffset = Vector2.Zero;
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var totalPhases = _config.BounceCount * 2;

        if (_phase >= totalPhases)
        {
            context.Output.CurrentBounceOffset = Vector2.Zero;
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        _timer += deltaTime * 4f;
        var bounceProgress = _timer % 1f;
        var bounceY = MathF.Sin(bounceProgress * MathF.PI) * _config.BounceHeight * MathF.Max(0f, 1f - _phase * 0.3f);
        context.Output.CurrentBounceOffset = new Vector2(0, -bounceY);

        if (_timer >= 1f)
        {
            _timer = 0f;
            _phase++;
        }

        return AnnouncementAnimationStatus.Running;
    }
}
