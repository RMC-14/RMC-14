using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.ElectronicSystem;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DropshipSpotlightComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField, AutoNetworkedField]
    public float Radius = 15;

    [DataField, AutoNetworkedField]
    public float Energy = 3;

    [DataField, AutoNetworkedField]
    public float Softness = 5;
}
