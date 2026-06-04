namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private bool WasRecentLizardTime(TimeSpan time, TimeSpan memory)
    {
        if (time <= TimeSpan.Zero)
            return false;

        var now = Timing.CurTime;
        if (time >= now)
            return true;

        return now - time <= memory;
    }
}
