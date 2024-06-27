using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableWeaponMeleeModsSystem))]
public sealed partial class AttachableWeaponMeleeModsToggleableComponent : Component
{
    [DataField, AutoNetworkedField]
    public AttachableWeaponMeleeModifierSet InactiveUnwielded = new();

    [DataField, AutoNetworkedField]
    public AttachableWeaponMeleeModifierSet InactiveWielded = new();

    [DataField, AutoNetworkedField]
    public AttachableWeaponMeleeModifierSet ActiveUnwielded = new();

    [DataField, AutoNetworkedField]
    public AttachableWeaponMeleeModifierSet ActiveWielded = new();
}
