using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRMCChemistrySystem))]
public sealed partial class RMCChemicalDispenserComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Energy;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxEnergy;

    [DataField, AutoNetworkedField]
    public FixedPoint2 CostPerUnit = 0.1;

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Network;

    [DataField, AutoNetworkedField]
    public string ContainerSlotId = "chemical_dispenser_slot";

    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype>[] Reagents =
    [
        "RMCAluminum", "RMCCarbon", "RMCChlorine", "RMCCopper", "RMCEthanol", "RMCFluorine",
        "RMCHydrogen", "RMCIron", "RMCLithium", "RMCMercury", "RMCNitrogen", "RMCOxygen",
        "RMCPhosphorus", "RMCPotassium", "RMCRadium", "RMCSilicon", "RMCSodium", "RMCSugar",
        "RMCSulfur", "RMCSulphuricAcid", "Water",
    ];

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<ReagentPrototype>> FreeReagents = ["Water"];

    [DataField, AutoNetworkedField]
    public FixedPoint2 DispenseSetting = 5;

    [DataField, AutoNetworkedField]
    public FixedPoint2[] Settings = [5, 10, 20, 30, 40];
}
