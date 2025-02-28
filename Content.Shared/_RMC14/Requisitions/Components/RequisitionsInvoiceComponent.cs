using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Requisitions.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRequisitionsSystem))]
public sealed partial class RequisitionsInvoiceComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Reward = 100;

    [DataField]
    public EntProtoId PaperOutput = "RMCPaperRequisitionInvoice";

    [DataField]
    public string? RequiredStamp = "paper_stamp-approve";
}
