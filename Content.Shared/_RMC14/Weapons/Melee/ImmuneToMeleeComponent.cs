using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Melee;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCMeleeWeaponSystem))]
public sealed partial class ImmuneToMeleeComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
