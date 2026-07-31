using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Rules;

[Serializable, NetSerializable]
public sealed class RMCScuttleCinematicEvent(TimeSpan startedAt, TimeSpan duration) : EntityEventArgs
{
    public readonly TimeSpan StartedAt = startedAt;
    public readonly TimeSpan Duration = duration;
}

public static class RMCScuttleCinematicTiming
{
    public static TimeSpan GetIntroShipDuration(TimeSpan duration)
    {
        duration = NonNegative(duration);
        return TimeSpan.FromSeconds(Math.Min(1.5, duration.TotalSeconds * 0.1));
    }

    public static TimeSpan GetIntroNukeDuration(TimeSpan duration)
    {
        duration = NonNegative(duration);
        return TimeSpan.FromSeconds(Math.Min(3.0, duration.TotalSeconds * 0.2));
    }

    public static TimeSpan GetSummaryDuration(TimeSpan duration)
    {
        duration = NonNegative(duration);
        return TimeSpan.FromSeconds(Math.Min(1.5, duration.TotalSeconds * 0.1));
    }

    public static TimeSpan GetExplosionOffset(TimeSpan duration)
    {
        duration = NonNegative(duration);
        return GetIntroShipDuration(duration) + GetIntroNukeDuration(duration);
    }

    private static TimeSpan NonNegative(TimeSpan time)
    {
        return time < TimeSpan.Zero ? TimeSpan.Zero : time;
    }
}
