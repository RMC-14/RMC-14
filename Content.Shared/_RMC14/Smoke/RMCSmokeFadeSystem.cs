using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Smoke;

public sealed class RMCSmokeFadeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public void SetLifetime(EntityUid uid, float seconds)
    {
        var fade = EnsureComp<RMCSmokeFadeComponent>(uid);
        var lifetime = TimeSpan.FromSeconds(seconds);
        fade.FadeEnd = _timing.CurTime + lifetime;
        fade.FadeStart = fade.FadeEnd - lifetime * fade.FadeFraction;
        Dirty(uid, fade);
    }

    public float GetAlpha(RMCSmokeFadeComponent fade, TimeSpan time)
    {
        if (time < fade.FadeStart || fade.FadeEnd <= fade.FadeStart)
            return fade.Alpha;

        var remaining = (float) ((fade.FadeEnd - time) / (fade.FadeEnd - fade.FadeStart));
        return fade.Alpha * Math.Clamp(remaining, 0f, 1f);
    }
}
