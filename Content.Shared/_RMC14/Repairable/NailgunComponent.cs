using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Repairable;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCRepairableSystem))]
public sealed partial class NailgunComponent : Component
{
    [DataField]
    public float NailingSpeed = 2;

    [DataField]
    public int MaterialPerRepair = 1;

    [DataField]
    public SoundSpecifier RepairSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/nailgun_repair_long.ogg");
}
