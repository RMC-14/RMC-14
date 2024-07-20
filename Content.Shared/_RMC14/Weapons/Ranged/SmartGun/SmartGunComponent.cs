using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.SmartGun;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMGunSystem))]
public sealed partial class SmartGunComponent : Component;
