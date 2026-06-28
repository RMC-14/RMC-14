using Content.Client._RMC14.Announce.Animations;
using Content.Client._RMC14.Announce.Effects;
using Content.Shared._RMC14.Announce;
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
        IReadOnlyList<RichTextLabel> labels,
        TimeSpan currentTime,
        float deltaTime)
    {
        if (IsFinished || _animation == null)
            return;

        var titleText = !string.IsNullOrEmpty(state.Data.Title) ? state.Data.Title : style.TitleConfig.Title;
        var hasTitle = style.TitleConfig.ShowTitle && !string.IsNullOrEmpty(titleText);

        var status = _animation.Update(animationContext, deltaTime);

        ResetBaseLabelColor(style, state, labels, hasTitle);
        if (status == AnnouncementAnimationStatus.Hold || status == AnnouncementAnimationStatus.Finished)
        {
            BeginHold(state, animationContext, currentTime);
        }
        else
        {
            state.State = AnnouncementState.Animating;
        }

        if (_holdStartedAt.HasValue)
        {
            var elapsedHold = (float) (currentTime - _holdStartedAt.Value).TotalSeconds;
            if (elapsedHold >= style.AnimationConfig.HoldDuration)
            {
                IsFinished = true;
                return;
            }
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

    private static void ResetBaseLabelColor(AnnouncementStyle style, ActiveAnnouncement state, IReadOnlyList<RichTextLabel> labels, bool hasTitle)
    {
        var alpha = state.FadeAlpha * state.PulseAlpha;
        for (var i = 0; i < labels.Count; i++)
        {
            var baseColor = hasTitle && i == 0
                ? style.TitleConfig.TitleColor
                : style.TextConfig.PrimaryColor;
            labels[i].Modulate = new Color(baseColor.R, baseColor.G, baseColor.B, baseColor.A * alpha);
        }

        foreach (var titleLabel in state.TitleLabels)
        {
            var baseColor = style.TitleConfig.TitleColor;
            titleLabel.Modulate = new Color(baseColor.R, baseColor.G, baseColor.B, baseColor.A * alpha);
        }
    }

    private void ApplyVisualEffects(
        AnnouncementStyle style,
        ActiveAnnouncement state,
        IReadOnlyList<RichTextLabel> labels,
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

