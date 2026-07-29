using Content.Shared._RMC14.Ghost;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Ghost;

/// <summary>
/// Owns the prepared, round-scoped materialized views used by the RMC ghost
/// target window. This component is intentionally server-only.
/// </summary>
[RegisterComponent, Access(typeof(RMCGhostTargetSystem))]
public sealed partial class RMCGhostTargetStoreComponent : Component
{
    internal readonly Dictionary<EntityUid, RMCGhostTargetRecord> Records = new();
    internal readonly Dictionary<EntityUid, HashSet<EntityUid>> MindTargets = new();
    internal readonly Dictionary<ProtoId<NpcFactionPrototype>, RMCGhostFactionSectionDefinition> FactionSections = new();
    internal readonly List<RMCGhostFactionSectionDefinition> FactionRoots = new();
    internal readonly Dictionary<RMCGhostTargetSectionKey, RMCGhostTargetStoredSection> Sections = new();
    internal readonly List<RMCGhostTargetStoredSection> SectionRoots = new();

    internal readonly RMCGhostTargetPreparedView Public = new();
    internal readonly RMCGhostTargetPreparedView Admin = new();

    public uint Revision;
    public bool DistressEndgame;
    public bool IsInitialized;
}

/// <summary>
/// Marks a body that has had a mind during the current round.
/// </summary>
[RegisterComponent]
public sealed partial class RMCGhostTargetTrackedComponent : Component;

internal enum RMCGhostTargetRecordKind : byte
{
    Body,
    Ghost,
    WarpPoint,
}

internal sealed class RMCGhostTargetRecord(
    EntityUid uid,
    RMCGhostTargetRecordKind kind,
    RMCGhostTargetEntry entry,
    EntityUid? mind,
    bool adminGhost)
{
    public readonly EntityUid Uid = uid;
    public RMCGhostTargetRecordKind Kind = kind;
    public RMCGhostTargetEntry Entry = entry;
    public EntityUid? Mind = mind;
    public bool AdminGhost = adminGhost;
    public readonly List<RMCGhostTargetMembership> Memberships = new();
}

internal sealed class RMCGhostTargetPreparedView
{
    public readonly List<RMCGhostTargetEntry> Targets = new();
    public readonly Dictionary<EntityUid, int> TargetIndices = new();
    public readonly HashSet<EntityUid> AllowedTargets = new();
    public List<RMCGhostTargetSection> Sections = new();

    public void Clear()
    {
        Targets.Clear();
        TargetIndices.Clear();
        AllowedTargets.Clear();
        Sections.Clear();
    }
}

internal sealed class RMCGhostFactionSectionDefinition(
    ProtoId<NpcFactionPrototype> id,
    LocId titleLocId,
    string? title,
    Color color)
{
    public readonly ProtoId<NpcFactionPrototype> Id = id;
    public readonly LocId TitleLocId = titleLocId;
    public readonly string? Title = title;
    public readonly Color Color = color;
    public readonly List<RMCGhostFactionSectionDefinition> Children = new();
}

internal readonly record struct RMCGhostTargetMembership(
    RMCGhostTargetSectionKey Section,
    int? SortValue = null);

internal readonly record struct RMCGhostTargetStoredEntry(
    EntityUid Uid,
    int? SortValue);

internal sealed class RMCGhostTargetStoredSection(
    RMCGhostTargetSectionKey key,
    LocId titleLocId,
    string? title,
    Color headerColor,
    bool isExpandedByDefault,
    bool isDynamic = false)
{
    public readonly RMCGhostTargetSectionKey Key = key;
    public readonly LocId TitleLocId = titleLocId;
    public string? Title = title;
    public Color HeaderColor = headerColor;
    public readonly bool IsExpandedByDefault = isExpandedByDefault;
    public readonly bool IsDynamic = isDynamic;
    public RMCGhostTargetStoredSection? Parent;
    public readonly List<RMCGhostTargetStoredEntry> Entries = new();
    public readonly List<RMCGhostTargetStoredSection> Children = new();
}
