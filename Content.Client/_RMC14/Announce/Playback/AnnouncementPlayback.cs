using Content.Client._RMC14.Announce.Animations;
using Content.Client._RMC14.Announce.Effects;
using Content.Shared._RMC14.Announce;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Announce;

public sealed class AnnouncementPlayback
{
    private IAnnouncementAnimation? _animation;
    private readonly List<IAnnouncementVisualEffect> _effects = new();
    private TimeSpan? _holdStartedAt;

    public bool IsFinished { get; private set; }

    public void Configure(
        IAnnouncementAnimation animation,
        IEnumerable<IAnnouncementVisualEffect> effects,
        AnnouncementAnimationContext context)
    {
        _animation = animation;
        _effects.Clear();
        _effects.AddRange(effects);
        _holdStartedAt = null;
        IsFinished = false;

        _animation.Reset(context);
    }

    public void Clear()
    {
        _animation = null;
        _effects.Clear();
        _holdStartedAt = null;
        IsFinished = false;
    }

    public void Update(
        AnnouncementAnimationContext animationContext,
        AnnouncementStyle style,
        ActiveAnnouncement state,
        IReadOnlyList<Control> labels,
        TimeSpan currentTime,
        float deltaTime)
    {
        if (IsFinished || _animation == null)
            return;

        var titleText = state.Data.Title;
        var hasTitle = style.TitleConfig.ShowTitle && !string.IsNullOrEmpty(titleText);

        var status = _animation.Update(animationContext, deltaTime);

        if (status == AnnouncementAnimationStatus.Hold || status == AnnouncementAnimationStatus.Finished)
        {
            BeginHold(state, animationContext, currentTime);
        }
        else
        {
            state.State = AnnouncementState.Animating;
        }

        var finished = false;
        if (_holdStartedAt.HasValue)
        {
            var elapsedHold = (float) (currentTime - _holdStartedAt.Value).TotalSeconds;
            var holdDuration = Math.Max(0f, style.AnimationConfig.HoldDuration);
            if (elapsedHold >= holdDuration)
            {
                var fadeOutDuration = Math.Max(0f, style.AnimationConfig.FadeOutDuration);
                if (fadeOutDuration <= 0f)
                {
                    finished = true;
                }
                else
                {
                    var elapsedFadeOut = elapsedHold - holdDuration;
                    state.State = AnnouncementState.FadingOut;
                    state.FadeAlpha = Math.Clamp(1f - elapsedFadeOut / fadeOutDuration, 0f, 1f);
                    finished = elapsedFadeOut >= fadeOutDuration;
                }
            }
        }

        ResetBaseLabelColor(style, state, labels, hasTitle);
        if (finished)
        {
            IsFinished = true;
            return;
        }

        ApplyVisualEffects(style, state, labels, currentTime, hasTitle);
    }

    private void BeginHold(ActiveAnnouncement state, AnnouncementAnimationContext context, TimeSpan currentTime)
    {
        if (_holdStartedAt.HasValue)
            return;

        _holdStartedAt = currentTime;
        state.State = AnnouncementState.Holding;
        context.SetAllLabels();
    }

    private static void ResetBaseLabelColor(AnnouncementStyle style, ActiveAnnouncement state, IReadOnlyList<Control> labels, bool hasTitle)
    {
        for (var i = 0; i < labels.Count; i++)
        {
            var baseColor = hasTitle && i == 0
                ? style.TitleConfig.TitleColor
                : style.TextConfig.PrimaryColor;
            // Color is already embedded in the markup or FontColorOverride.
            labels[i].Modulate = new Color(1f, 1f, 1f, baseColor.A);
        }

        foreach (var titleLabel in state.TitleLabels)
        {
            var baseColor = style.TitleConfig.TitleColor;
            titleLabel.Modulate = new Color(1f, 1f, 1f, baseColor.A);
        }
    }

    private void ApplyVisualEffects(
        AnnouncementStyle style,
        ActiveAnnouncement state,
        IReadOnlyList<Control> labels,
        TimeSpan currentTime,
        bool hasTitle)
    {
        if (_effects.Count == 0)
            return;

        var effectContext = new AnnouncementEffectContext(style, state, labels, hasTitle);
        foreach (var effect in _effects)
        {
            effect.Apply(effectContext, currentTime);
        }
    }
}

