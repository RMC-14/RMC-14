using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Battery;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCGunBatterySystem))]
public sealed partial class BatteryInGunComponent : Component;
