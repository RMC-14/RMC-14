using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VehicleFlamerTankSlotsComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MaxTanks = 1;

    [DataField]
    public EntProtoId? StartingItem;
}
