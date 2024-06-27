using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableWeaponMeleeModsSystem))]
public sealed partial class AttachableWeaponMeleeModsComponent : Component
{
    [DataField, AutoNetworkedField]
    public AttachableWeaponMeleeModifierSet Modifiers = new();
}
