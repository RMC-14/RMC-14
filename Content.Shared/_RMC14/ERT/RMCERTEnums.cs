using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ERT;

[Serializable, NetSerializable]
public enum RMCERTRequestSource : byte
{
    Console,
    Handheld,
    Admin,
    Ares,
}

[Serializable, NetSerializable]
public enum RMCERTRequestState : byte
{
    Requested,
    PendingAdmin,
    PendingDispatch,
    Recruiting,
    Spawning,
    Launching,
    Arrived,
    Completed,
    Denied,
    Cancelled,
    Failed,
}
