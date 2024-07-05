using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Entrenching;

[RegisterComponent, NetworkedComponent]
[Access(typeof(BarricadeSandbagComponent))]
public sealed partial class BarricadeSandbagComponent : Component;
