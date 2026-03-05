using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace Content.Shared._RMC14.Xenonids.ResinMark;

[Serializable, NetSerializable]
public enum XenoResinMarkUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public readonly record struct XenoResinMarkType(EntProtoId Id, string Name, string Description);

[Serializable, NetSerializable]
public readonly record struct XenoResinPlacedMark(NetEntity Marker, string Name, string LocationName);

[Serializable, NetSerializable]
public sealed class XenoResinMarkBuiState(
    EntProtoId selectedType,
    List<XenoResinMarkType> types,
    List<XenoResinPlacedMark> marks,
    bool canForceTrack) : BoundUserInterfaceState
{
    public readonly EntProtoId SelectedType = selectedType;
    public readonly List<XenoResinMarkType> Types = types;
    public readonly List<XenoResinPlacedMark> Marks = marks;
    public readonly bool CanForceTrack = canForceTrack;
}

[Serializable, NetSerializable]
public sealed class XenoResinMarkSelectTypeBuiMsg(EntProtoId type) : BoundUserInterfaceMessage
{
    public readonly EntProtoId Type = type;
}

[Serializable, NetSerializable]
public sealed class XenoResinMarkWatchBuiMsg(NetEntity marker) : BoundUserInterfaceMessage
{
    public readonly NetEntity Marker = marker;
}

[Serializable, NetSerializable]
public sealed class XenoResinMarkDestroyBuiMsg(NetEntity marker) : BoundUserInterfaceMessage
{
    public readonly NetEntity Marker = marker;
}

[Serializable, NetSerializable]
public sealed class XenoResinMarkForceTrackBuiMsg(NetEntity marker) : BoundUserInterfaceMessage
{
    public readonly NetEntity Marker = marker;
}
