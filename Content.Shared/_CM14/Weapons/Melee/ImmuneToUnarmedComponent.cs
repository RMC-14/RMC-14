using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Weapons.Melee;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMMeleeWeaponSystem))]
public sealed partial class ImmuneToUnarmedComponent : Component;
