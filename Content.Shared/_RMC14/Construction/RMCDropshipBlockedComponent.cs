using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCConstructionSystem))]
public sealed partial class RMCDropshipBlockedComponent : Component
{
    [DataField, AutoNetworkedField]
    public string FixtureId = "rmc_dropship_blocked";
}
