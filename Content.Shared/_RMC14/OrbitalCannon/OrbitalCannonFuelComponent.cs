using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.OrbitalCannon;

[RegisterComponent, NetworkedComponent]
[Access(typeof(OrbitalCannonSystem))]
public sealed partial class OrbitalCannonFuelComponent : Component;
