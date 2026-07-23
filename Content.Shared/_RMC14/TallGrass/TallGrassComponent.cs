using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.TallGrass;

[RegisterComponent, NetworkedComponent]
[Access(typeof(TallGrassSystem))]
public sealed partial class TallGrassComponent : Component
{
}
