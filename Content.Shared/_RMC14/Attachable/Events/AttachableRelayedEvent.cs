namespace Content.Shared._RMC14.Attachable.Events;

/// <summary>
/// Wrapper for events relayed to attachables by their holder.
/// </summary>
public sealed class AttachableRelayedEvent<TEvent> : EntityEventArgs
{
    public TEvent Args;
    public EntityUid Holder;

    public AttachableRelayedEvent(TEvent args, EntityUid holder)
    {
        Args = args;
        Holder = holder;
    }
}
