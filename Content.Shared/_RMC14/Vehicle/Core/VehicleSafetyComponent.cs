using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleSafetySystem))]
public sealed partial class VehicleSafetyComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;
}
