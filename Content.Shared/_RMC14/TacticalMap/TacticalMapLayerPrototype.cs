using System.Collections.Generic;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[Prototype("tacticalMapLayer"), Serializable, NetSerializable]
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
    public string? UpdateAnnouncement;

    [DataField]
    public TacticalMapLayerAnnouncementTarget AnnouncementTarget = TacticalMapLayerAnnouncementTarget.None;

    [DataField]
    public List<EntProtoId<IFFFactionComponent>>? IffFactions;
}

[Serializable, NetSerializable]
public enum TacticalMapLayerAnnouncementTarget : byte
{
    None,
    Marines,
    Xenos
}
