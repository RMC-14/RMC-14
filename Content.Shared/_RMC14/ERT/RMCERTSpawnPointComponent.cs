namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Spawn marker metadata used to place responders on the shuttle before seat assignment.
/// </summary>
[RegisterComponent]
public sealed partial class RMCERTSpawnPointComponent : Component
{
    /// <summary>
    /// Role tags this spawn point is best suited for.
    /// </summary>
    [DataField]
    public List<string> RoleTags = [];

    /// <summary>
    /// Seat tags copied to seat assignment when a responder spawns here.
    /// </summary>
    [DataField]
    public List<string> SeatTags = [];

    /// <summary>
    /// Higher-priority spawn points are considered before lower-priority points.
    /// </summary>
    [DataField]
    public int Priority;
}
