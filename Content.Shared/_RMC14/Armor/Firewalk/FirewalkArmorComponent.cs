using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Armor.Firewalk;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FirewalkArmorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist Whitelist = new();

    [DataField, AutoNetworkedField]
    public ComponentRegistry AddComponentsOnFirewalk;

    [DataField, AutoNetworkedField]
    public ComponentRegistry AddComponentsOnEquip;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionFireShield";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public SlotFlags Slots = SlotFlags.OUTERCLOTHING;

    [DataField, AutoNetworkedField]
    public TimeSpan FirewalkTime = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public Color AuraColor;
}
