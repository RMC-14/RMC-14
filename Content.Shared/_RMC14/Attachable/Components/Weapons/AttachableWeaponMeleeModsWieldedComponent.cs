using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableWeaponMeleeModsSystem))]
public sealed partial class AttachableWeaponMeleeModsWieldedComponent : Component
{
    [DataField, AutoNetworkedField]
    public AttachableWeaponMeleeModifierSet Unwielded = new();

    [DataField, AutoNetworkedField]
    public AttachableWeaponMeleeModifierSet Wielded = new();
}
