using Content.Shared.Roles;
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

    [DataField]
    public string? TakeOne;

    [DataField(required: true)]
    public List<CMVendorEntry> Entries = new();

    // Only used by Spec Vendors to mark the kit section for RMCVendorSpecialistComponent logic.
    [DataField]
    public int? SharedSpecLimit;

    [DataField]
    public List<ProtoId<JobPrototype>> Jobs = new();

    [DataField]
    public List<string> Holidays = new();

    [DataField]
    public bool HasBoxes;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial record CMVendorEntry
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

    [DataField]
    public int? Multiplier;

    [DataField]
    public int? Max;

    [DataField]
    public List<EntProtoId> LinkedEntries = new();

    [DataField, AutoNetworkedField]
    public EntProtoId? Box;

    [DataField, AutoNetworkedField]
    public int? BoxAmount;

    [DataField, AutoNetworkedField]
    public int? BoxSlots;
}
