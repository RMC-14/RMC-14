using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Armor;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCBulkyArmorSystem))]
public sealed partial class RMCUserBulkyArmorIncapableComponent : Component;
