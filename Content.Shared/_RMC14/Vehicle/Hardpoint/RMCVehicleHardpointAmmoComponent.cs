using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCVehicleHardpointAmmoComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MagazineSize = 1;

    [DataField, AutoNetworkedField]
    public int MaxStoredMagazines = 0;

    [DataField, AutoNetworkedField]
    public int StoredMagazines = 0;
}
