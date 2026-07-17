using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Ghost;

[Serializable, NetSerializable]
public sealed class RMCGhostWarpsRequestEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class RMCGhostWarpsResponseEvent : EntityEventArgs
{
    public RMCGhostWarpsResponseEvent(List<RMCGhostTargetSection> sections)
    {
        Sections = sections;
    }

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
        int index,
        LocId titleLocId,
        string? title,
        Color headerColor,
        bool isExpandedByDefault,
        List<RMCGhostTargetEntry> entries,
        List<RMCGhostTargetSection> children)
    {
        Index = index;
        TitleLocId = titleLocId;
        Title = title;
        HeaderColor = headerColor;
        IsExpandedByDefault = isExpandedByDefault;
        Entries = entries;
        Children = children;
    }

    public int Index { get; }

    public LocId TitleLocId { get; }

    public string? Title { get; }

    public Color HeaderColor { get; }

    public bool IsExpandedByDefault { get; }

    public List<RMCGhostTargetEntry> Entries { get; }

    public List<RMCGhostTargetSection> Children { get; }
}

[Serializable, NetSerializable]
public readonly struct RMCGhostTargetEntry
{
    public RMCGhostTargetEntry(
        NetEntity entity,
        string displayName,
        string searchText,
        string? displayJob,
        bool isWarpPoint,
        int followerCount,
        SpriteSpecifier.Rsi? healthIcon,
        int healthPercent,
        SpriteSpecifier.Rsi? tacticalIcon,
        SpriteSpecifier.Rsi? tacticalBackground,
        RMCGhostTargetTooltipJobKind tooltipJobKind)
    {
        Entity = entity;
        DisplayName = displayName;
        SearchText = searchText;
        DisplayJob = displayJob;
        IsWarpPoint = isWarpPoint;
        FollowerCount = followerCount;
        HealthIcon = healthIcon;
        HealthPercent = healthPercent;
        TacticalIcon = tacticalIcon;
        TacticalBackground = tacticalBackground;
        TooltipJobKind = tooltipJobKind;
    }

    public NetEntity Entity { get; }

    public string DisplayName { get; }

    public string SearchText { get; }

    public string? DisplayJob { get; }

    public bool IsWarpPoint { get; }

    public int FollowerCount { get; }

    public SpriteSpecifier.Rsi? HealthIcon { get; }

    public int HealthPercent { get; }

    public SpriteSpecifier.Rsi? TacticalIcon { get; }

    public SpriteSpecifier.Rsi? TacticalBackground { get; }

    public RMCGhostTargetTooltipJobKind TooltipJobKind { get; }
}

[Serializable, NetSerializable]
public enum RMCGhostTargetTooltipJobKind : byte
{
    None,
    Job,
    Caste,
}
