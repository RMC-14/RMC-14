using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Ghost;

[RegisterComponent]
public sealed partial class GhostRoleApplySpecialComponent : Component
{
    [DataField]
    public EntProtoId? Squad;
}
