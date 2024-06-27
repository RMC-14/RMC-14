using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableWeaponRangedModsSystem))]
public sealed partial class AttachableWeaponRangedModsWieldedComponent : Component
{
    [DataField, AutoNetworkedField]
    public AttachableWeaponRangedModifierSet Unwielded = new();

    [DataField, AutoNetworkedField]
    public AttachableWeaponRangedModifierSet Wielded = new();
}
