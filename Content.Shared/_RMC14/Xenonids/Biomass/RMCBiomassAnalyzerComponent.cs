using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Biomass;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRMCBiomassAnalyzerDataSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class RMCBiomassAnalyzerComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Points;

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<RMCBiomassUpgradeComponent>, int> PurchaseCounts = new();
}
