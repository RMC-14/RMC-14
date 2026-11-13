using Robust.Shared.Localization;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Requisitions;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RequisitionsCategory
{
    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public List<RequisitionsEntry> Entries = new();
}
