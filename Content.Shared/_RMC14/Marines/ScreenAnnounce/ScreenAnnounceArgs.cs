using Content.Shared.Shuttles.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Marines.ScreenAnnounce;

[NetSerializable, Serializable]
public sealed class ScreenAnnounceArgs
{
    public float PrintSpeed;
    public float ShakeIntensity;
    public float FlickerChance;
    public float GlitchChance;
    public float HoldDuration;
    public float FadeDuration;
    public float LineHeightUnscaled;
    public float MaxTextWidthFraction;

    public ScreenAnnounceArgs()
    {
        PrintSpeed = 0.03f;
        ShakeIntensity = 0.8f;
        FlickerChance = 0.02f;
        GlitchChance = 0.01f;
        HoldDuration = 3f;
        FadeDuration = 1.5f;
        LineHeightUnscaled = 40f;
        MaxTextWidthFraction = 0.9f;
    }

    public ScreenAnnounceArgs(
        float printSpeed,
        float shakeIntensity,
        float flickerChance,
        float glitchChance,
        float holdDuration,
        float fadeDuration,
        float lineHeightUnscaled,
        float maxTextWidthFraction)
    {
        PrintSpeed = printSpeed;
        ShakeIntensity = shakeIntensity;
        FlickerChance = flickerChance;
        GlitchChance = glitchChance;
        HoldDuration = holdDuration;
        FadeDuration = fadeDuration;
        LineHeightUnscaled = lineHeightUnscaled;
        MaxTextWidthFraction = maxTextWidthFraction;
    }

    public ScreenAnnounceArgs With(
        float? printSpeed = null,
        float? shakeIntensity = null,
        float? flickerChance = null,
        float? glitchChance = null,
        float? holdDuration = null,
        float? fadeDuration = null,
        float? lineHeightUnscaled = null,
        float? maxTextWidthFraction = null)
    {
        return new ScreenAnnounceArgs(
            printSpeed ?? PrintSpeed,
            shakeIntensity ?? ShakeIntensity,
            flickerChance ?? FlickerChance,
            glitchChance ?? GlitchChance,
            holdDuration ?? HoldDuration,
            fadeDuration ?? FadeDuration,
            lineHeightUnscaled ?? LineHeightUnscaled,
            maxTextWidthFraction ?? MaxTextWidthFraction
        );
    }

    public static readonly ScreenAnnounceArgs Default = new();
}
