using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCComponentsSystem))]
public sealed partial class RemoveComponentsComponent : Component
{
    [DataField]
    public ComponentRegistry Components = new();
}
