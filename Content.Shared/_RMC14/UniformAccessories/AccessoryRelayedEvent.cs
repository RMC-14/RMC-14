namespace Content.Shared._RMC14.UniformAccessories;

/// <summary>
/// Wrapper for events relayed to accessories by their holder.
/// </summary>
public sealed class AccessoryRelayedEvent<TEvent> : EntityEventArgs
{
    public TEvent Args;
    public EntityUid Holder;


    public AccessoryRelayedEvent(TEvent args, EntityUid holder)
    {
        Args = args;
        Holder = holder;
    }
}
