using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Welding;

[RegisterComponent, NetworkedComponent]
public sealed partial class WeldingVisionComponent : Component
{
    [DataField("superior")]
    public bool Superior = false;
}
