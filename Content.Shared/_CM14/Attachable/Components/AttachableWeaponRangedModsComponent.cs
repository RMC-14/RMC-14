using Robust.Shared.GameStates;


namespace Content.Shared._CM14.Attachable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAttachableWeaponRangedModsSystem))]
public sealed partial class AttachableWeaponRangedModsComponent : Component
{
    [DataField("unwielded"), AutoNetworkedField]
    public AttachableWeaponRangedModifierSet ModifiersUnwielded = new AttachableWeaponRangedModifierSet();
    
    [DataField("wielded"), AutoNetworkedField]
    public AttachableWeaponRangedModifierSet ModifiersWielded = new AttachableWeaponRangedModifierSet();
}
