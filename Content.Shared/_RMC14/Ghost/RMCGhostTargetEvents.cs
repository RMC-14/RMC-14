using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ghost;

[Serializable, NetSerializable]
public sealed class RMCGhostWarpsRequestEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class RMCGhostWarpsResponseEvent : EntityEventArgs
{
    public RMCGhostWarpsResponseEvent(List<RMCGhostWarp> warps)
    {
        Warps = warps;
    }

    public List<RMCGhostWarp> Warps { get; }
}

[Serializable, NetSerializable]
public sealed class RMCGhostWarpToTargetRequestEvent : EntityEventArgs
{
    public RMCGhostWarpToTargetRequestEvent(NetEntity target)
    {
        Target = target;
    }

    public NetEntity Target { get; }
}

[Serializable, NetSerializable]
public sealed class RMCGhostnadoRequestEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public readonly struct RMCGhostWarp
{
    public RMCGhostWarp(
        NetEntity entity,
        string displayName,
        string? displayJob,
        bool isWarpPoint,
        int followerCount,
        string? healthState,
        int healthPercent)
    {
        Entity = entity;
        DisplayName = displayName;
        DisplayJob = displayJob;
        IsWarpPoint = isWarpPoint;
        FollowerCount = followerCount;
        HealthState = healthState;
        HealthPercent = healthPercent;
    }

    public NetEntity Entity { get; }

    public string DisplayName { get; }

    public string? DisplayJob { get; }

    public bool IsWarpPoint { get; }

    public int FollowerCount { get; }

    public string? HealthState { get; }

    public int HealthPercent { get; }
}
