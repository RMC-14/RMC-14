namespace Content.Shared._RMC14.Visor;

[ByRefEvent]
public sealed class ActivateVisorAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid User;

    public ActivateVisorAttemptEvent(EntityUid user)
    {
        this.User = user;
    }
}
