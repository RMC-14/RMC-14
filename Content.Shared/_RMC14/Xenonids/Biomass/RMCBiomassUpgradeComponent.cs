using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Biomass;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCBiomassAnalyzerDataSystem))]
public sealed partial class RMCBiomassUpgradeComponent : Component
{
    [DataField(required: true)]
    public RMCBiomassUpgradeCategory Category;

    [DataField(required: true)]
    public int Cost;

    [DataField]
    public int RequiredClearance = 1;

    [DataField]
    public int PriceChange;

    [DataField]
    public int MinimumPrice;

    [DataField]
    public int MaximumPrice = int.MaxValue;

    [DataField(required: true)]
    public EntProtoId PrintedItem;
}
