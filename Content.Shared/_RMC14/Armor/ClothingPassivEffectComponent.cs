using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Armor;

[RegisterComponent, NetworkedComponent]
public sealed partial class ClothingGrantComponentsComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();
}
