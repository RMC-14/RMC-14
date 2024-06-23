using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Entrenching;

[RegisterComponent, NetworkedComponent]
[Access(typeof(BarricadeComponent))]
public sealed partial class BarricadeComponent : Component;
