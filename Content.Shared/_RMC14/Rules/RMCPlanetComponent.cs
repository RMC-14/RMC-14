using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Rules;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCPlanetComponent : Component
{
    [DataField]
    public Vector2i Offset;
}
