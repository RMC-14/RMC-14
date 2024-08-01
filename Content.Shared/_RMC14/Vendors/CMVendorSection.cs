using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vendors;

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

    // Only used by Spec Vendors to mark the kit section for RMCVendorSpecialistComponent logic.
    [DataField]
    public int? SharedSpecLimit;
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

    [DataField]
    public bool Recommended;
}
