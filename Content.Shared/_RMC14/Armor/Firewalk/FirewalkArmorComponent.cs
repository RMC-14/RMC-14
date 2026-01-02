using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Armor.Firewalk;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(FirewalkSystem))]
public sealed partial class FirewalkArmorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist Whitelist = new();

    [DataField(required: true)]
    public ComponentRegistry AddComponentsOnFirewalk = new();

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionFireShield";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public SlotFlags Slots = SlotFlags.OUTERCLOTHING;

    [DataField, AutoNetworkedField]
    public TimeSpan FirewalkTime = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public Color AuraColor = Color.Teal;
}
