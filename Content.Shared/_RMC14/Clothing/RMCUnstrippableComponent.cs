using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Clothing;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCUnstrippableComponent : Component
{
    [DataField]
    public bool PoliceCanStrip = true;
}
