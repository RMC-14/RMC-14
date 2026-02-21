using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VehicleTurretVisualComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity Turret;
}
