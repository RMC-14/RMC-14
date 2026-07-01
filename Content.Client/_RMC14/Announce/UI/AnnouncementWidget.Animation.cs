using System.Numerics;
using Content.Client._RMC14.Announce.Animations;
using Content.Client._RMC14.Announce.Effects;
using Content.Shared._RMC14.Announce;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Announce;

public sealed partial class AnnouncementWidget
{
    private readonly AnnouncementPlayback _playback = new();
    private AnnouncementAnimationContext? _animationContext;

    private void ConfigureAnimationAndEffects()
    {
        if (ActiveAnnouncement == null)
            return;

        var animation = AnnouncementAnimationFactory.Create(ActiveAnnouncement.ResolvedStyle);
        var effects = AnnouncementEffectsRegistry.BuildEffects(ActiveAnnouncement.ResolvedStyle);
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
            ActiveAnnouncement.ResolvedStyle,
            ActiveAnnouncement,
            _richTextLabels,
            currentTime,
            deltaTime);
        if (_playback.IsFinished && !PreviewMode)
        {
            FinishAnnouncement();
        }
    }

    private AnnouncementAnimationContext CreateAnimationContext()
    {
        return new AnnouncementAnimationContext(
            ActiveAnnouncement!,
            ActiveAnnouncement!.ResolvedStyle,
            ActiveAnnouncement!.Data.Text,
            ActiveAnnouncement!.CleanText,
            _richTextLabels,
            _titleOffset,
            CreateFormattedMessage,
            SetAllLabelsText,
            _spriteContainer,
            _random);
    }

    private void SetAllLabelsText()
    {
        if (ActiveAnnouncement == null)
            return;

        var text = ActiveAnnouncement.Data.Text;
        var style = ActiveAnnouncement.ResolvedStyle;

        for (var i = _titleOffset; i < _richTextLabels.Length; i++)
        {
            var textIndex = i - _titleOffset;
            if (textIndex < text.Length)
            {
                var message = CreateFormattedMessage(text[textIndex], style);
                (_richTextLabels[i] as RichTextLabel)?.SetMessage(message);
            }
        }
    }
}

