using Content.Shared.Stacks;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._RMC14.Repairable;

[RegisterComponent]
[Access(typeof(RMCRepairableSystem))]
public sealed partial class NailgunRepairableComponent : Component
{
    [DataField]
    public float RepairMetal = 0;

    [DataField]
    public float RepairPlasteel = 0;

    [DataField]
    public float RepairWood = 0;
}
