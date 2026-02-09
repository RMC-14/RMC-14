using System.Collections.Generic;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[Prototype(TacticalMapPrototypeIds.Layer), Serializable, NetSerializable]
public sealed partial class TacticalMapLayerPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name { get; set; } = default!;

    [DataField]
    public string? LogName;

    [DataField]
    public bool DefaultVisible;

    [DataField]
    public int SortOrder;

    [DataField]
    public TacticalMapLayerKind Kind = TacticalMapLayerKind.Generic;

    [DataField]
    public string? Group;

    [DataField]
    public ProtoId<TacticalMapLayerPrototype>? ParentLayer;

    [DataField]
    public TacticalMapLayerVisibility Visibility = TacticalMapLayerVisibility.All;

    [DataField]
    public bool RequiresParentVisible;

    [DataField]
    public EntProtoId<SquadTeamComponent>? SquadId;

    [DataField]
    public bool CanDraw = true;

    [DataField]
    public bool CanUpdateBlips;

    [DataField]
    public bool CanContainBlips = true;

    [DataField]
    public string? UpdateAnnouncement;

    [DataField]
    public TacticalMapLayerAnnouncementTarget AnnouncementTarget = TacticalMapLayerAnnouncementTarget.None;

    [DataField]
    public List<EntProtoId<IFFFactionComponent>>? IffFactions;
}

[Serializable, NetSerializable]
public enum TacticalMapLayerKind : byte
{
    Generic,
    Global,
    Squad,
    Faction
}

[Serializable, NetSerializable]
public enum TacticalMapLayerVisibility : byte
{
    All,
    SquadMembers,
    LayerAccess,
    SquadOrAccess,
    None
}

[Serializable, NetSerializable]
public enum TacticalMapLayerAnnouncementTarget : byte
{
    None,
    Marines,
    Xenos
}
