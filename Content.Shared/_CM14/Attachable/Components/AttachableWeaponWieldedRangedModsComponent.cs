using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableWeaponRangedModsSystem))]
public sealed partial class AttachableWeaponWieldedRangedModsComponent : Component
{
    [DataField, AutoNetworkedField]
    public AttachableWeaponRangedModifierSet Unwielded = new();

    [DataField, AutoNetworkedField]
    public AttachableWeaponRangedModifierSet Wielded = new();
}
