using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCVehicleSpotlightComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField, AutoNetworkedField]
    public float Radius = 12f;

    [DataField, AutoNetworkedField]
    public float Energy = 2.5f;

    [DataField, AutoNetworkedField]
    public float Softness = 2f;
}
