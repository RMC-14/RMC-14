using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.NightVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedNightVisionSystem))]
public sealed partial class NightVisionItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "CMActionToggleScoutVision";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public EntityUid? User;

    [DataField, AutoNetworkedField]
    public bool Toggleable = true;

    // Only allows for a single slotflag right now because some code uses strings and some code uses enums to determine slots :(
    [DataField, AutoNetworkedField]
    public SlotFlags SlotFlags { get; set; } = SlotFlags.EYES;
}
