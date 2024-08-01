using Content.Shared._RMC14.Attachable.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableModifiersSystem))]
public sealed partial class AttachableWeaponRangedModsComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<AttachableWeaponRangedModifierSet> Modifiers = new();

    [DataField, AutoNetworkedField]
    public List<AttachableWeaponFireModesModifierSet>? FireModeMods;
}
