using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class GunRequiresSkillsComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Skills Skills;
}
