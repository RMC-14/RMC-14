using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.SmartGun;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SmartGunSystem))]
public sealed partial class SmartGunBatteryComponent : Component;
