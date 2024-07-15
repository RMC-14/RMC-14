using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor.Magnetic;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCMagneticSystem))]
public sealed partial class RMCMagneticItemComponent : Component;
