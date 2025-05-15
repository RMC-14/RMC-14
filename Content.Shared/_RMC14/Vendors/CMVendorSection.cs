﻿using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

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

    /// <summary>
    /// New title that will be applied to the marine when this item is purchased.
    /// </summary>
    [DataField]
    public LocId? JobTitle;

    /// <summary>
    /// If true, JobTitle will be appended to the marine's current title. If false - replaces the current title.
    /// </summary>
    [DataField]
    public bool IsAppendTitle = false;

    /// <summary>
    /// New icon that will be applied to the marine when this item is purchased.
    /// </summary>
    [DataField]
    public SpriteSpecifier.Rsi? Icon;

    [DataField]
    public SpriteSpecifier.Rsi? MapBlip;
}
