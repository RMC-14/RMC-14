using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

[Serializable, NetSerializable]
public sealed class JoinLarvaQueueRequest : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class LeaveLarvaQueueRequest : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class LarvaQueueStatusEvent : EntityEventArgs
{
    public int QueuePosition { get; }
    public int TotalInQueue { get; }
    public int AvailableLarvae { get; }
    public int PendingLarvae { get; }
    public bool InQueue { get; }

    public LarvaQueueStatusEvent(int queuePosition, int totalInQueue, int availableLarvae, int pendingLarvae, bool inQueue)
    {
        QueuePosition = queuePosition;
        TotalInQueue = totalInQueue;
        AvailableLarvae = availableLarvae;
        PendingLarvae = pendingLarvae;
        InQueue = inQueue;
    }
}

[Serializable, NetSerializable]
public sealed class LarvaQueueStatusRequest : EntityEventArgs;

[Serializable, NetSerializable]
public record JoinLarvaQueueEvent(NetEntity Hive);

[ByRefEvent]
public record struct LarvaReadyToBurstEvent(EntityUid Host, EntityUid Larva);

[ByRefEvent]
public record struct BurstLarvaConsumedEvent(EntityUid Larva);

[ByRefEvent]
public record struct ScanExistingLarvaeEvent();
