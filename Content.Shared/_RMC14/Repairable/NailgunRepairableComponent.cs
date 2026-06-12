using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Repairable;

[RegisterComponent]
[Access(typeof(RMCRepairableSystem))]
public sealed partial class NailgunRepairableComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<StackPrototype>, float> RepairValues = new();

    [DataField]
    public Dictionary<ProtoId<StackPrototype>, float> WallRepairFractions = new();
}
