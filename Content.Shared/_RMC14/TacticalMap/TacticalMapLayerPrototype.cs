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

    /// <summary>
    /// Display name or localization key shown in the UI.
    /// </summary>
    [DataField(required: true)]
    public string Name { get; set; } = default!;

    /// <summary>
    /// Optional short name used for admin logs.
    /// </summary>
    [DataField]
    public string? LogName;

    /// <summary>
    /// Whether this layer should be visible by default if no layers are specified.
    /// </summary>
    [DataField]
    public bool DefaultVisible;

    /// <summary>
    /// Sort order for UI layer selector.
    /// </summary>
    [DataField]
    public int SortOrder;

    /// <summary>
    /// Optional announcement message when this layer is updated.
    /// </summary>
    [DataField]
    public string? UpdateAnnouncement;

    /// <summary>
    /// Controls who receives the announcement when this layer is updated.
    /// </summary>
    [DataField]
    public TacticalMapLayerAnnouncementTarget AnnouncementTarget = TacticalMapLayerAnnouncementTarget.None;

    /// <summary>
    /// IFF factions that grant visibility of this layer.
    /// </summary>
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
