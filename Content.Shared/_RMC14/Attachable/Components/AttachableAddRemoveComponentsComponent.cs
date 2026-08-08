using Content.Shared._RMC14.Attachable.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(AttachableHolderSystem))]
public sealed partial class AttachableAddRemoveComponentsComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = default!;
}
