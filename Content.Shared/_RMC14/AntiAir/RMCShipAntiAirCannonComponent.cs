using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.AntiAir;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCShipAntiAirSystem))]
public sealed partial class RMCShipAntiAirCannonComponent : Component;
