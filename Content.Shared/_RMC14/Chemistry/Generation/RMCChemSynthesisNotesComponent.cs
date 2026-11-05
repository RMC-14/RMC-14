using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Generation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCChemSynthesisNotesComponent : Component
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
    public bool FullReport;

    [DataField, AutoNetworkedField]
    public bool Unique;

    [DataField, AutoNetworkedField]
    public int? Tier;
}
