using Content.Shared._RMC14.Dropship;

namespace Content.Client._RMC14.Dropship;

[Access(typeof(DropshipNavigationBui))]
public sealed partial class DropshipNavigationComputerComponent : SharedDropshipNavigationComputerComponent
{
    // This is identical to SharedDropshipNavigationComputerComponent.DoorGroupLockStates, but is needed for access reasons
    public new Dictionary<string, bool> DoorGroupLockStates
    {
        get => base.DoorGroupLockStates;
        set => base.DoorGroupLockStates = value;
    }
}

