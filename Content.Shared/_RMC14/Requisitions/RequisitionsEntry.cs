using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Requisitions;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RequisitionsEntry
{
    [DataField]
    public string? Name;

    [DataField(required: true)]
    public int Cost;

    [DataField(required: true)]
    public EntProtoId Crate;

    [DataField]
    public List<EntProtoId> Entities = new();
}
