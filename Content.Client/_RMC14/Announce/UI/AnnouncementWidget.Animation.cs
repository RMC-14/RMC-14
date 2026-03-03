using Content.Client._RMC14.Announce.Animations;
using Content.Client._RMC14.Announce.Effects;
using Content.Shared._RMC14.Announce;
using System.Numerics;

namespace Content.Client._RMC14.Announce;

public sealed partial class AnnouncementWidget
{
    private readonly AnnouncementPlayback _playback = new();
    private AnnouncementAnimationContext? _animationContext;

    private void ConfigureAnimationAndEffects()
    {
        if (ActiveAnnouncement == null)
            return;

        var animation = AnnouncementAnimationFactory.Create(ActiveAnnouncement.Data.Style, _random);
        var effects = AnnouncementEffectsRegistry.BuildEffects(ActiveAnnouncement.Data.Style);
        _animationContext = CreateAnimationContext();
        _playback.Configure(animation, effects, _animationContext);
    }

    private void UpdateAnnouncement(float deltaTime, TimeSpan currentTime)
    {
        if (ActiveAnnouncement == null)
            return;

        if (_animationContext == null)
            return;

        _playback.Update(
            _animationContext,
            ActiveAnnouncement.Data.Style,
            ActiveAnnouncement,
            _richTextLabels,
            currentTime,
            deltaTime);
        if (_playback.IsFinished)
        {
            FinishAnnouncement();
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
            _spriteContainer,
            _random);
    }

    private Vector2 GetSlideStartPosition(AnnouncementStyle style)
    {
        if (style.AnimationConfig.Animation != AnnouncementAnimation.Slide)
            return Vector2.Zero;

        var screenSize = ResolveScreenSize();

        return style.AnimationConfig.AnimationEnhancements?.SlideFrom switch
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

