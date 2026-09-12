namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private bool WasRecentLizardTime(TimeSpan time, TimeSpan memory)
    {
        var now = Timing.CurTime;
        return time > TimeSpan.Zero && (time >= now || now - time <= memory);
    }
}
