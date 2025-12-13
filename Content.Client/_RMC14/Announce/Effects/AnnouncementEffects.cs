using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Announce;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Client._RMC14.Announce.Effects;

public static class AnnouncementEffects
{
    public static void UpdateSlideAnimation(ActiveAnnouncement announcement, float deltaTime)
    {
        var style = announcement.Data.Style.AnimationEnhancements;
        var progress = Math.Min(1f, announcement.SlideTimer / style.SlideDuration);

        var easedProgress = ApplyEasing(progress, style.EasingIn);
        announcement.CurrentSlideOffset = Vector2.Lerp(announcement.SlideStartPosition, Vector2.Zero, easedProgress);

        announcement.SlideTimer += deltaTime;

        if (progress >= 1f)
        {
            announcement.CurrentLine = announcement.Data.Text.Length;
            announcement.State = AnnouncementState.Holding;
        }
    }

    public static void UpdateZoomAnimation(ActiveAnnouncement announcement, float deltaTime)
    {
        var style = announcement.Data.Style.AnimationEnhancements;
        var progress = Math.Min(1f, announcement.ZoomTimer / style.ZoomDuration);

        var easedProgress = ApplyEasing(progress, style.EasingIn);
        announcement.ZoomCurrentScale = MathHelper.Lerp(style.ZoomStartScale, 1f, easedProgress);

        announcement.ZoomTimer += deltaTime;

        if (progress >= 1f)
        {
            announcement.CurrentLine = announcement.Data.Text.Length;
            announcement.State = AnnouncementState.Holding;
        }
    }

    public static void UpdateBounceAnimation(ActiveAnnouncement announcement, float deltaTime)
    {
        var style = announcement.Data.Style.AnimationEnhancements;

        if (announcement.BouncePhase < style.BounceCount * 2)
        {
            announcement.BounceTimer += deltaTime * 4f;
            var bounceProgress = announcement.BounceTimer % 1f;
            var bounceHeight = MathF.Sin(bounceProgress * MathF.PI) * style.BounceHeight * (1f - announcement.BouncePhase * 0.3f);
            announcement.CurrentBounceOffset = new Vector2(0, -bounceHeight);

            if (announcement.BounceTimer >= 1f)
            {
                announcement.BounceTimer = 0f;
                announcement.BouncePhase++;
            }
        }
        else
        {
            announcement.CurrentLine = announcement.Data.Text.Length;
            announcement.State = AnnouncementState.Holding;
        }
    }

    public static void UpdateTypewriterAnimation(ActiveAnnouncement announcement, float deltaTime, IRobustRandom random)
    {
        var style = announcement.Data.Style;
        var animationStyle = style.AnimationEnhancements;
        var text = announcement.Data.Text;

        if (announcement.CurrentLine >= text.Length)
        {
            announcement.State = AnnouncementState.Holding;
            return;
        }

        var currentLineText = announcement.CleanText[announcement.CurrentLine];
        var baseSpeed = style.PrintSpeed;

        if (animationStyle.RandomizeSpeed)
        {
            baseSpeed *= random.NextFloat(0.5f, 1.5f);
        }

        var speedMultiplier = animationStyle.TypewriterMode switch
        {
            TypewriterStyle.Burst => GetBurstSpeedMultiplier(announcement.CurrentChar, currentLineText.Length),
            TypewriterStyle.Random => random.NextFloat(0.3f, 2.0f),
            _ => 1.0f
        };

        announcement.TypewriterTimer += deltaTime;
        var requiredTime = baseSpeed * speedMultiplier;

        if (announcement.TypewriterTimer >= requiredTime)
        {
            announcement.TypewriterTimer = 0f;
            announcement.CurrentChar++;

            if (announcement.CurrentChar >= currentLineText.Length)
            {
                announcement.CurrentLine++;
                announcement.CurrentChar = 0;

                if (announcement.CurrentLine >= text.Length)
                {
                    announcement.State = AnnouncementState.Holding;
                }
            }
        }
    }

    public static void UpdateGlitchAnimation(ActiveAnnouncement announcement, float deltaTime)
    {
        var style = announcement.Data.Style;
        announcement.GlitchTimer += deltaTime;

        if (announcement.GlitchTimer >= 0.1f)
        {
            announcement.GlitchTimer = 0f;

            if (Random.Shared.NextFloat() < style.GlitchChance)
            {
                ApplyGlitchEffect(announcement);
            }
            else
            {
                announcement.GlitchText = null;
            }
        }

        announcement.CurrentLine = announcement.Data.Text.Length;
        announcement.State = AnnouncementState.Holding;
    }

    public static void UpdateFadeAnimation(ActiveAnnouncement announcement, float deltaTime)
    {
        var fadeDuration = 2.0f;
        var progress = Math.Min(1f, announcement.FadeTimer / fadeDuration);

        announcement.FadeAlpha = ApplyEasing(progress, EasingType.EaseIn);
        announcement.FadeTimer += deltaTime;

        if (progress >= 1f)
        {
            announcement.CurrentLine = announcement.Data.Text.Length;
            announcement.State = AnnouncementState.Holding;
        }
    }

    public static void UpdatePulseAnimation(ActiveAnnouncement announcement, float deltaTime)
    {
        var pulseSpeed = 4.0f;
        announcement.PulseTimer += deltaTime * pulseSpeed;

        var pulseValue = (MathF.Sin(announcement.PulseTimer) + 1f) * 0.5f;
        announcement.PulseScale = MathHelper.Lerp(0.8f, 1.2f, pulseValue);
        announcement.PulseAlpha = MathHelper.Lerp(0.7f, 1.0f, pulseValue);

        announcement.CurrentLine = announcement.Data.Text.Length;
        announcement.State = AnnouncementState.Holding;
    }

    private static float GetBurstSpeedMultiplier(int currentChar, int totalLength)
    {
        var progress = (float)currentChar / totalLength;

        if (progress < 0.3f)
            return 0.5f;
        else if (progress < 0.7f)
            return 2.0f;
        else
            return 0.8f;
    }

    private static void ApplyGlitchEffect(ActiveAnnouncement announcement)
    {
        var glitchChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
        var originalText = announcement.Data.Text;
        announcement.GlitchText = new string[originalText.Length];

        for (int i = 0; i < originalText.Length; i++)
        {
            var line = originalText[i];
            var glitchedChars = new char[line.Length];

            for (int j = 0; j < line.Length; j++)
            {
                if (Random.Shared.NextFloat() < 0.1f)
                {
                    var glitchIndex = Random.Shared.Next(glitchChars.Length);
                    glitchedChars[j] = glitchChars[glitchIndex];
                }
                else
                {
                    glitchedChars[j] = line[j];
                }
            }

            announcement.GlitchText[i] = new string(glitchedChars);
        }
    }

    private static float ApplyEasing(float t, EasingType easing)
    {
        return easing switch
        {
            EasingType.Linear => t,
            EasingType.EaseIn => t * t,
            EasingType.EaseOut => 1f - (1f - t) * (1f - t),
            EasingType.EaseInOut => t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t),
            EasingType.BounceIn => 1f - BounceOut(1f - t),
            EasingType.BounceOut => BounceOut(t),
            EasingType.Elastic => ElasticOut(t),
            _ => t
        };
    }

    private static float BounceOut(float t)
    {
        if (t < 1f / 2.75f)
        {
            return 7.5625f * t * t;
        }
        else if (t < 2f / 2.75f)
        {
            t -= 1.5f / 2.75f;
            return 7.5625f * t * t + 0.75f;
        }
        else if (t < 2.5f / 2.75f)
        {
            t -= 2.25f / 2.75f;
            return 7.5625f * t * t + 0.9375f;
        }
        else
        {
            t -= 2.625f / 2.75f;
            return 7.5625f * t * t + 0.984375f;
        }
    }

    private static float ElasticOut(float t)
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;

        var p = 0.3f;
        var s = p / 4f;

        return MathF.Pow(2f, -10f * t) * MathF.Sin((t - s) * (2f * MathF.PI) / p) + 1f;
    }
}
