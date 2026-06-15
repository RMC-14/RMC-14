using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.AntiAir;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCAlmayerAntiAirSystem))]
public sealed partial class RMCAlmayerAntiAirCannonComponent : Component;
