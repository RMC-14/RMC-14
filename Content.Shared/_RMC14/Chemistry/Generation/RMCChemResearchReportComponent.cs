using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.Generation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCChemResearchReportComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype>? ReagentId;

    [DataField, AutoNetworkedField]
    public List<ChemReportProperty> Properties = new();

    [DataField, AutoNetworkedField]
    public int Overdose;

    [DataField, AutoNetworkedField]
    public int CriticalOverdose;

    [DataField, AutoNetworkedField]
    public bool Completed;

    [DataField, AutoNetworkedField]
    public bool IsGenerated;

    [DataField, AutoNetworkedField]
    public bool SimulationFailed;

    [DataField, AutoNetworkedField]
    public bool SimulatorReport;

    [DataField, AutoNetworkedField]
    public int SampleNumber;

    [DataField, AutoNetworkedField]
    public bool SavedEmissionSpectrum;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ChemReportProperty
{
    [DataField]
    public string PropertyId = string.Empty;

    [DataField]
    public int Level;
}
