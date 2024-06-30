using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableSpeedModsSystem))]
public sealed partial class AttachableSpeedModsToggleableComponent : Component
{
    [DataField, AutoNetworkedField]
    public AttachableSpeedModifierSet InactiveUnwielded = new();

    [DataField, AutoNetworkedField]
    public AttachableSpeedModifierSet InactiveWielded = new();

    [DataField, AutoNetworkedField]
    public AttachableSpeedModifierSet ActiveUnwielded = new();

    [DataField, AutoNetworkedField]
    public AttachableSpeedModifierSet ActiveWielded = new();
}

