using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCChemistrySystem))]
public sealed partial class RMCSolutionTransferWhitelistComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId Popup;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? SourceWhitelist;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? TargetWhitelist;
}
