using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Armor.Plates;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCArmorPlateComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry WearerComponents = new();
}
