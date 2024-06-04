using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Vendors;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class CMVendorSection
{
    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField]
    public (string Id, int Amount)? Choices;

    [DataField]
    public string? TakeAll;

    [DataField(required: true)]
    public List<CMVendorEntry> Entries = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class CMVendorEntry
{
    [DataField(required: true)]
    public EntProtoId Id;

    [DataField]
    public string? Name;

    [DataField]
    public int? Amount;

    [DataField]
    public int? Points;

    [DataField]
    public int Spawn = 1;
}
