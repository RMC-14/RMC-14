using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Requisitions;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RequisitionsCategory
{
    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public List<RequisitionsEntry> Entries = new();
}
