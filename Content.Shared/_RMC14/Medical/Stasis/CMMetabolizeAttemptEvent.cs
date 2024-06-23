namespace Content.Shared._RMC14.Medical.Stasis;

[ByRefEvent]
public record struct CMMetabolizeAttemptEvent(bool Cancelled)
{
    public void Cancel()
    {
        Cancelled = true;
    }
}
