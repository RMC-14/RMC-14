using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Content.Shared._RMC14.Dialog;

namespace Content.Shared._RMC14.Xenonids.Banish;

[Serializable, NetSerializable]
public sealed class ManageHiveBanishEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class ManageHiveBanishChooseXenoEvent(NetEntity xeno, NetUserId? player) : EntityEventArgs
{
    public NetEntity Xeno = xeno;

    /// <summary>
    /// The player controlling the xenonid when the list was opened.
    /// </summary>
    public NetUserId? Player = player;
}

[Serializable, NetSerializable]
public sealed record ManageHiveBanishReasonEvent(NetEntity Xeno, NetUserId? Player, string Message = "") : DialogInputEvent(Message);

[Serializable, NetSerializable]
public sealed class ManageHiveReadmitEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class ManageHiveReadmitXenoEvent(NetEntity xeno) : EntityEventArgs
{
    public NetEntity Xeno = xeno;
}

[Serializable, NetSerializable]
public sealed class ManageHiveReadmitConfirmEvent(NetEntity xeno) : EntityEventArgs
{
    public NetEntity Xeno = xeno;
}
