using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Requisitions;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RequisitionsEntry
{
    [DataField]
    public LocId? Name;

    [DataField]
    public LocId? Description;

    [DataField]
    public SpriteSpecifier? Icon;

    [DataField(required: true)]
    public int Cost;

    [DataField(required: true)]
    public EntProtoId Crate;

    [DataField]
    public List<EntProtoId> Entities = new();
}
