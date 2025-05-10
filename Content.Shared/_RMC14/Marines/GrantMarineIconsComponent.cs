using Content.Shared.Inventory;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines;

/// <summary>
///     Will give <see cref="ShowMarineIconsComponent"/> to the mob that equips this item.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMarineSystem))]
public sealed partial class GrantMarineIconsComponent : Component, IClothingSlots
{
    [DataField, AutoNetworkedField]
    public SlotFlags Slots { get; set; } = SlotFlags.EARS;

    [DataField, AutoNetworkedField]
    public List<ProtoId<NpcFactionPrototype>>? Factions;

    [DataField, AutoNetworkedField]
    public bool BypassFactionIcons = false;
}
