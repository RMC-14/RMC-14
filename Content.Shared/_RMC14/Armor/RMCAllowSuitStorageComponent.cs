using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMArmorSystem))]
public sealed partial class RMCAllowSuitStorageComponent : Component;
