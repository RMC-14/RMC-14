using Content.Shared.Armor;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Armor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMArmorSystem))]
public sealed partial class RMCAllowSuitStorageUserWhitelistComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist DefaultWhitelist = new();

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId<AllowSuitStorageComponent> AllowedWhitelist;

    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist? User;
}
