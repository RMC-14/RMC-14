using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Construction.Upgrades;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCUpgradeSystem))]
public sealed partial class RMCConstructionUpgradeItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist Whitelist;
}
