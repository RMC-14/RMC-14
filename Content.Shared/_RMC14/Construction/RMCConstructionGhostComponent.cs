using Content.Shared._RMC14.Construction.Prototypes;

namespace Content.Shared._RMC14.Construction;

[RegisterComponent]
public sealed partial class RMCConstructionGhostComponent : Component
{
    public int GhostId { get; set; } = -1;
    public RMCConstructionPrototype? Prototype { get; set; }
    public int Amount { get; set; } = 1;
}
