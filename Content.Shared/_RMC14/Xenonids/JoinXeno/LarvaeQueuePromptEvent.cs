using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

[Serializable, NetSerializable]
public sealed class LarvaPromptEvent : EntityEventArgs
{
    public NetEntity Larva { get; }
    public NetEntity Hive { get; }
    public TimeSpan TimeoutAt { get; }

    public LarvaPromptEvent(NetEntity larva, NetEntity hive, TimeSpan timeoutAt)
    {
        Larva = larva;
        Hive = hive;
        TimeoutAt = timeoutAt;
    }
}

[Serializable, NetSerializable]
public sealed class AcceptLarvaPromptRequest : EntityEventArgs
{
    public NetEntity Larva { get; }

    public AcceptLarvaPromptRequest(NetEntity larva)
    {
        Larva = larva;
    }
}

[Serializable, NetSerializable]
public sealed class DeclineLarvaPromptRequest : EntityEventArgs
{
    public NetEntity Larva { get; }

    public DeclineLarvaPromptRequest(NetEntity larva)
    {
        Larva = larva;
    }
}

[Serializable, NetSerializable]
public sealed class LarvaPromptCancelledEvent : EntityEventArgs
{
    public NetEntity Larva { get; }
    public string Reason { get; }

    public LarvaPromptCancelledEvent(NetEntity larva, string reason = "timeout")
    {
        Larva = larva;
        Reason = reason;
    }
}
