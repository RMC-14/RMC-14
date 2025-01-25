using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Telephone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTelephoneSystem))]
public sealed partial class RotaryPhoneBackpackComponent : Component
{
    [DataField, AutoNetworkedField]
    public SlotFlags Slot = SlotFlags.BACK;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionTelephone";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;
}
