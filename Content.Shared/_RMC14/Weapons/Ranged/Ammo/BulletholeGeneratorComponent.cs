using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Ammo;

[RegisterComponent, NetworkedComponent]
[Access(typeof(BulletholeSystem))]
public sealed partial class BulletholeGeneratorComponent : Component;
