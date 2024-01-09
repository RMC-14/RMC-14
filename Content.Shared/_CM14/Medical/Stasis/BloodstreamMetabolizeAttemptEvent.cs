namespace Content.Shared._CM14.Medical.Stasis;

[ByRefEvent]
public record struct BloodstreamMetabolizeAttemptEvent(bool Cancelled)
{
    public void Cancel()
    {
        Cancelled = false;
    }
}
