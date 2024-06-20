using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableWeaponRangedModsSystem))]
public sealed partial class AttachableWeaponRangedModsToggleableComponent : Component
{
    [DataField, AutoNetworkedField]
    public AttachableWeaponRangedModifierSet InactiveUnwielded = new();

    [DataField, AutoNetworkedField]
    public AttachableWeaponRangedModifierSet InactiveWielded = new();

    [DataField, AutoNetworkedField]
    public AttachableWeaponRangedModifierSet ActiveUnwielded = new();

    [DataField, AutoNetworkedField]
    public AttachableWeaponRangedModifierSet ActiveWielded = new();
}
