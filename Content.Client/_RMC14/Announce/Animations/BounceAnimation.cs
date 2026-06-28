using System;
using System.Numerics;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class BounceAnimation : IAnnouncementAnimation
{
    private float _timer;
    private int _phase;

    public void Reset(AnnouncementAnimationContext context)
    {
        _timer = 0f;
        _phase = 0;
        context.Output.CurrentBounceOffset = Vector2.Zero;
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var enhancements = context.Style.AnimationConfig.AnimationEnhancements;
        var bounceCount = enhancements?.BounceCount ?? 3;
        var bounceHeight = enhancements?.BounceHeight ?? 15f;
        var totalPhases = bounceCount * 2;

        if (_phase >= totalPhases)
        {
            context.Output.CurrentBounceOffset = Vector2.Zero;
            context.SetAllLabels();
            return AnnouncementAnimationStatus.Finished;
        }

        _timer += deltaTime * 4f;
        var bounceProgress = _timer % 1f;
        var bounceY = MathF.Sin(bounceProgress * MathF.PI) * bounceHeight * MathF.Max(0f, 1f - _phase * 0.3f);
        context.Output.CurrentBounceOffset = new Vector2(0, -bounceY);

        if (_timer >= 1f)
        {
            _timer = 0f;
            _phase++;
        }

        return AnnouncementAnimationStatus.Running;
    }
}
