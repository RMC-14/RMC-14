using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor.Plates;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCArmorPlateSystem))]
public sealed partial class RMCCoagulatorPlateActiveComponent : Component;
