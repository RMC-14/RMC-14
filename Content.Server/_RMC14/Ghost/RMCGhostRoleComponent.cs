using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Ghost;

[RegisterComponent]
[Access([typeof(RMCGhostRoleSystem)])]
public sealed partial class RMCGhostRoleComponent : Component
{
    /// <summary>
    /// Components to be added to the spawned entity.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry AddComponents = new();

    /// <summary>
    /// How many people can take this role.
    ///
    /// null = infinite
    /// </summary>
    [DataField]
    public int? Remaining;
}
