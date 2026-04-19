using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle.Viewport;


[RegisterComponent, NetworkedComponent]
public sealed partial class VehicleViewportComponent : Component;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VehicleViewportUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? PreviousTarget;

    [DataField, AutoNetworkedField]
    public EntityUid? Source;
}
