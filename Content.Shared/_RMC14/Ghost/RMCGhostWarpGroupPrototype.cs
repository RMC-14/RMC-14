using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ghost;

/// <summary>
/// Прототип группы для группировки целей варпа.
/// </summary>
[Prototype("rmcGhostWarpGroup")]
public sealed partial class RMCGhostWarpGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; } = string.Empty;

    [DataField]
    public Color Color { get; private set; } = Color.FromHex("#696969");

    [DataField]
    public bool IsExpandedByDefault { get; private set; } = true;

    /// <summary>
    /// List of subgroups of this group. Subgroups will be nested in this group.
    /// </summary>
    ///
    /// <remarks>
    /// - Any nesting is supported (tree of arbitrary depth).
    /// - Cycles (A -> B -> C -> A) are strictly prohibited, this will lead to an error and ignoring the subgroup.
    /// - It is not recommended to specify the same subgroup in several parents (there will be duplication in the UI).
    /// - Only those groups that are not subgroups of others are considered root.
    /// </remarks>
    [DataField]
    public List<ProtoId<RMCGhostWarpGroupPrototype>>? Subgroups { get; private set; }
}
