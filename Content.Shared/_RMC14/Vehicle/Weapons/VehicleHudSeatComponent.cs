using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(VehicleWeaponsSystem))]
public sealed partial class VehicleHudSeatComponent : Component
{
    [AutoNetworkedField]
    public VehicleWeaponsUiState Ui = new(default, new List<VehicleWeaponsUiEntry>(), false, false, false, false, false);
}
