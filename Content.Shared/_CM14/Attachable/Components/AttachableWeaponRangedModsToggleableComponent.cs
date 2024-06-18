using Robust.Shared.GameStates;


namespace Content.Shared._CM14.Attachable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAttachableWeaponRangedModsSystem))]
public sealed partial class AttachableWeaponRangedModsToggleableComponent : Component
{
    [DataField("inactiveUnwielded"), AutoNetworkedField]
    public AttachableWeaponRangedModifierSet ModifiersInactiveUnwielded = new AttachableWeaponRangedModifierSet();
    
    [DataField("inactiveWielded"), AutoNetworkedField]
    public AttachableWeaponRangedModifierSet ModifiersInactiveWielded = new AttachableWeaponRangedModifierSet();
    
    [DataField("activeUnwielded"), AutoNetworkedField]
    public AttachableWeaponRangedModifierSet ModifiersActiveUnwielded = new AttachableWeaponRangedModifierSet();
    
    [DataField("activeWielded"), AutoNetworkedField]
    public AttachableWeaponRangedModifierSet ModifiersActiveWielded = new AttachableWeaponRangedModifierSet();
}
