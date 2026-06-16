using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlamerSystem))]
public sealed partial class RMCBroilerComponent : Component
{
    [DataField, AutoNetworkedField]
    public SlotFlags Slot = SlotFlags.BACK;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionBroiler";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public string ContainerPrefix = "Tank";

    [DataField, AutoNetworkedField]
    public int ActiveTank = 0;

    [DataField, AutoNetworkedField]
    public ResPath NumberingResource = new("Effects/crayondecals.rsi");
}
