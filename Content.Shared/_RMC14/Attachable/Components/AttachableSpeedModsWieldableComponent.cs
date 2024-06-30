using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableSpeedModsSystem))]
public sealed partial class AttachableSpeedModsWieldableComponent : Component
{
    [DataField, AutoNetworkedField]
    public AttachableSpeedModifierSet Unwielded = new();

    [DataField, AutoNetworkedField]
    public AttachableSpeedModifierSet Wielded = new();
}

