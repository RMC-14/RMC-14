using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Spawners;

/// <summary>
/// Overrides ProportionalSpawnerComponent prototypes if they're on a map with this.
/// </summary>

[RegisterComponent]
public sealed partial class MapProportionalSpawnsComponent : Component
{
    [DataField]
    public List<EntProtoId> Prototypes { get; set; } = new();
}
