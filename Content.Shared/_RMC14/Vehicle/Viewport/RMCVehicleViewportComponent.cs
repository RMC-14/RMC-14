using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle.Viewport;


[RegisterComponent, NetworkedComponent]
public sealed partial class RMCVehicleViewportComponent : Component;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCVehicleViewportUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? PreviousTarget;
}
