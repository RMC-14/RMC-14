using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared.Roles;
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
    public List<ProtoId<RankPrototype>> Ranks = new();

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
    /// New role name that will be applied to the marine when this item is purchased.
    /// </summary>
    [DataField]
    public LocId? GiveSquadRoleName;

    /// <summary>
    /// If true, RoleName will be appended to the marine's current role name. If false - replaces the current role name.
    /// </summary>
    [DataField]
    public bool IsAppendSquadRoleName = false;

    /// <summary>
    /// New prefix that will be applied to the marine when this item is purchased.
    /// </summary>
    [DataField]
    public LocId? GivePrefix;

    /// <summary>
    /// If true, Prefix will be appended to the marine's current prefix. If false - replaces the current prefix.
    /// </summary>
    [DataField]
    public bool IsAppendPrefix = false;

    /// <summary>
    /// New icon that will be applied to the marine when this item is purchased.
    /// </summary>
    [DataField]
    public SpriteSpecifier.Rsi? GiveIcon;

    [DataField]
    public SpriteSpecifier.Rsi? GiveMapBlip;
}
