using Content.Client._RMC14.Announce.Animations;
using Content.Client._RMC14.Announce.Effects;
using Content.Shared._RMC14.Announce;
using Robust.Client.UserInterface;
using System.Numerics;

namespace Content.Client._RMC14.Announce;

public sealed partial class AnnouncementWidget
{
    private IAnnouncementAnimation? _animation;
    private readonly List<IAnnouncementVisualEffect> _effects = new();
    private TimeSpan? _animationCompletedAt;

    private void ConfigureAnimationAndEffects()
    {
        if (ActiveAnnouncement == null)
            return;

        _animation = AnnouncementAnimationFactory.Create(ActiveAnnouncement.Data.Style, _random);
        _effects.Clear();
        _effects.AddRange(AnnouncementEffectsRegistry.BuildEffects(ActiveAnnouncement.Data.Style));
        _animationCompletedAt = null;

        _animation?.Reset(CreateAnimationContext());
    }

    private void UpdateAnnouncement(float deltaTime, TimeSpan currentTime)
    {
        if (ActiveAnnouncement == null)
            return;

        if (ActiveAnnouncement.State == AnnouncementState.Animating && _animation != null)
        {
            var ctx = CreateAnimationContext();
            var finished = _animation.Update(ctx, deltaTime);
            if (finished)
            {
                ActiveAnnouncement.State = AnnouncementState.Holding;
                _animationCompletedAt = currentTime;
                SetAllLabelsText();
            }
        }
        else if (ActiveAnnouncement.State == AnnouncementState.Holding && _animationCompletedAt.HasValue)
        {
            var holdDuration = ActiveAnnouncement.Data.Style.HoldDuration;
            var elapsedHold = (float) (currentTime - _animationCompletedAt.Value).TotalSeconds;
            if (elapsedHold >= holdDuration)
            {
                FinishAnnouncement();
                return;
            }
        }

        ApplyVisualEffects(currentTime);
    }

    private void ApplyVisualEffects(TimeSpan currentTime)
    {
        if (ActiveAnnouncement == null)
            return;

        foreach (var label in _richTextLabels)
        {
            label.Modulate = ActiveAnnouncement.Data.Style.PrimaryColor;
        }

        var effectContext = new AnnouncementEffectContext(
            ActiveAnnouncement.Data.Style,
            ActiveAnnouncement,
            _richTextLabels);

        foreach (var effect in _effects)
        {
            effect.Apply(effectContext, currentTime);
        }
    }

    private AnnouncementAnimationContext CreateAnimationContext()
    {
        return new AnnouncementAnimationContext(
            ActiveAnnouncement!,
            ActiveAnnouncement!.Data.Style,
            ActiveAnnouncement!.Data.Text,
            ActiveAnnouncement!.CleanText,
            _richTextLabels,
            _titleOffset,
            CreateFormattedMessage,
            SetAllLabelsText,
            _random);
    }

    private Vector2 GetSlideStartPosition(AnnouncementStyle style)
    {
        if (style.AnimationEnhancements?.EnableSlide != true)
            return Vector2.Zero;

        var screenSize = Parent is UIScreen screen ? screen.Size : new Vector2(1920, 1080);

        return style.AnimationEnhancements.SlideFrom switch
        {
            SlideDirection.Left => new Vector2(-screenSize.X, 0),
            SlideDirection.Right => new Vector2(screenSize.X, 0),
            SlideDirection.Top => new Vector2(0, -screenSize.Y),
            SlideDirection.Bottom => new Vector2(0, screenSize.Y),
            _ => Vector2.Zero
        };
    }

    private void SetAllLabelsText()
    {
        if (ActiveAnnouncement == null)
            return;

        var originalText = ActiveAnnouncement.Data.Text;
        var style = ActiveAnnouncement.Data.Style;

        for (var i = _titleOffset; i < _richTextLabels.Length; i++)
        {
            var textIndex = i - _titleOffset;
            if (textIndex < originalText.Length)
            {
                var message = CreateFormattedMessage(originalText[textIndex], style);
                _richTextLabels[i].SetMessage(message);
            }
        }
    }
}
