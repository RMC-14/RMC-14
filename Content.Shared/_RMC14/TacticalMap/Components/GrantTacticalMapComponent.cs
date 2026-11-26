using Content.Shared.Inventory;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class GrantTacticalMapComponent : Component, IClothingSlots
{
    [DataField, AutoNetworkedField]
    public SlotFlags Slots { get; set; } = SlotFlags.EARS;

    [DataField, AutoNetworkedField]
    public bool GrantMap = true;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionOpenTacticalMapMarine";

    [DataField, AutoNetworkedField]
    public List<ProtoId<NpcFactionPrototype>> Factions = new();
}
