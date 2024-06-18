using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Weapons.Ranged.Whitelist;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMGunSystem))]
public sealed partial class GrenadeSpecWhitelistComponent : Component;
