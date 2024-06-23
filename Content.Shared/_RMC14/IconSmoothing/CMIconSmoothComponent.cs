using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.IconSmoothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CMIconSmoothComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Smooth;
}
