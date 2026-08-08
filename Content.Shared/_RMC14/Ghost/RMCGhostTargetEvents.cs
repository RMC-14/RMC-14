using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Ghost;

[Serializable, NetSerializable]
public sealed class RMCGhostWarpsRequestEvent : EntityEventArgs
{
    public RMCGhostWarpsRequestEvent(uint requestId)
    {
        RequestId = requestId;
    }

    public uint RequestId { get; }
}

[Serializable, NetSerializable]
public sealed class RMCGhostWarpsResponseEvent : EntityEventArgs
{
    public RMCGhostWarpsResponseEvent(
        uint requestId,
        uint revision,
        NetEntity self,
        List<RMCGhostTargetEntry> targets,
        List<RMCGhostTargetSection> sections)
    {
        RequestId = requestId;
        Revision = revision;
        Self = self;
        Targets = targets;
        Sections = sections;
    }

    public uint RequestId { get; }

    public uint Revision { get; }

    public NetEntity Self { get; }

    public List<RMCGhostTargetEntry> Targets { get; }

    public List<RMCGhostTargetSection> Sections { get; }
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
public sealed class RMCGhostTargetSection
{
    public RMCGhostTargetSection(
        RMCGhostTargetSectionKey key,
        LocId titleLocId,
        string? title,
        Color headerColor,
        bool isExpandedByDefault,
        List<NetEntity> targets,
        List<RMCGhostTargetSection> children)
    {
        Key = key;
        TitleLocId = titleLocId;
        Title = title;
        HeaderColor = headerColor;
        IsExpandedByDefault = isExpandedByDefault;
        Targets = targets;
        Children = children;
    }

    public RMCGhostTargetSectionKey Key { get; }

    public LocId TitleLocId { get; }

    public string? Title { get; }

    public Color HeaderColor { get; }

    public bool IsExpandedByDefault { get; }

    public List<NetEntity> Targets { get; }

    public List<RMCGhostTargetSection> Children { get; }
}

[Serializable, NetSerializable]
public readonly record struct RMCGhostTargetSectionKey(
    RMCGhostTargetSectionKind Kind,
    string? Prototype = null,
    NetEntity? Entity = null);

[Serializable, NetSerializable]
public readonly struct RMCGhostTargetEntry
{
    public RMCGhostTargetEntry(
        NetEntity entity,
        string displayName,
        string? displayJob,
        RMCGhostTargetFlags flags,
        int followerCount,
        RMCGhostTargetHealthState healthState,
        byte healthPercent,
        SpriteSpecifier.Rsi? tacticalIcon,
        SpriteSpecifier.Rsi? tacticalBackground,
        RMCGhostTargetTooltipJobKind tooltipJobKind)
    {
        Entity = entity;
        DisplayName = displayName;
        DisplayJob = displayJob;
        Flags = flags;
        FollowerCount = followerCount;
        HealthState = healthState;
        HealthPercent = healthPercent;
        TacticalIcon = tacticalIcon;
        TacticalBackground = tacticalBackground;
        TooltipJobKind = tooltipJobKind;
    }

    public NetEntity Entity { get; }

    public string DisplayName { get; }

    public string? DisplayJob { get; }

    public RMCGhostTargetFlags Flags { get; }

    public int FollowerCount { get; }

    public RMCGhostTargetHealthState HealthState { get; }

    public byte HealthPercent { get; }

    public SpriteSpecifier.Rsi? TacticalIcon { get; }

    public SpriteSpecifier.Rsi? TacticalBackground { get; }

    public RMCGhostTargetTooltipJobKind TooltipJobKind { get; }

    public bool IsWarpPoint => (Flags & RMCGhostTargetFlags.WarpPoint) != 0;
}

[Serializable, NetSerializable]
public enum RMCGhostTargetSectionKind : byte
{
    Marines,
    Xenos,
    Infected,
    Survivors,
    Escaped,
    Faction,
    Others,
    MarineOthers,
    Dead,
    WarpPoints,
    Ghosts,
    Squad,
    Cryo,
}

[Flags]
[Serializable, NetSerializable]
public enum RMCGhostTargetFlags : byte
{
    None = 0,
    WarpPoint = 1 << 0,
}

[Serializable, NetSerializable]
public enum RMCGhostTargetHealthState : byte
{
    None,
    High,
    Medium,
    Low,
}

[Serializable, NetSerializable]
public enum RMCGhostTargetTooltipJobKind : byte
{
    None,
    Job,
    Caste,
}
