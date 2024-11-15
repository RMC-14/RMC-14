using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Weapons.Ranged.Ammo;

[RegisterComponent, NetworkedComponent]
[Access(typeof(BulletholeVisualsSystem))]
public sealed partial class BallisticAmmoComponent : Component;
