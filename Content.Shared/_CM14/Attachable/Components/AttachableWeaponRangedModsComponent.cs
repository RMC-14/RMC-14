using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableWeaponRangedModsSystem))]
public sealed partial class AttachableWeaponRangedModsComponent : Component
{
    [DataField, AutoNetworkedField]
    public AttachableWeaponRangedModifierSet Modifiers = new();
}
