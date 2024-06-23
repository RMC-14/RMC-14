using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Whitelist;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class GunUserWhitelistComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist Whitelist = new();
}
