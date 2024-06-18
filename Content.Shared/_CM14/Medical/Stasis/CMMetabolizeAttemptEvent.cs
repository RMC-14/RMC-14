namespace Content.Shared._CM14.Medical.Stasis;

[ByRefEvent]
public record struct CMMetabolizeAttemptEvent(bool Cancelled)
{
    public void Cancel()
    {
        Cancelled = true;
    }
}
