using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Repairable;

[RegisterComponent]
[Access(typeof(RMCRepairableSystem))]
public sealed partial class RMCRepairableReplaceComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Prototype = default!;
}
