using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared._RMC14.Movement;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableTemporarySpeedModsSystem))]
public sealed partial class AttachableTemporarySpeedModsComponent : Component
{
    [DataField, AutoNetworkedField]
    public AttachableAlteredType Alteration = AttachableAlteredType.Interrupted;

    [DataField, AutoNetworkedField]
    public List<TemporarySpeedModifierSet> Modifiers = new();
}


