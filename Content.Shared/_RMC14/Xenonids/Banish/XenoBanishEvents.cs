using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Banish;

[Serializable, NetSerializable]
public sealed class ManageHiveBanishEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class ManageHiveBanishXenoEvent : EntityEventArgs
{
    public NetEntity Xeno;

    public ManageHiveBanishXenoEvent(NetEntity xeno)
    {
        Xeno = xeno;
    }
}

[Serializable, NetSerializable]
public sealed class ManageHiveBanishConfirmEvent : EntityEventArgs
{
    public NetEntity Xeno;

    public ManageHiveBanishConfirmEvent(NetEntity xeno)
    {
        Xeno = xeno;
    }
}

[Serializable, NetSerializable]
public sealed record ManageHiveBanishMessageEvent(NetEntity Xeno, string Message) : DialogInputEvent(Message);

[Serializable, NetSerializable]
public sealed class ManageHiveReadmitEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class ManageHiveReadmitXenoEvent : EntityEventArgs
{
    public NetEntity Xeno;

    public ManageHiveReadmitXenoEvent(NetEntity xeno)
    {
        Xeno = xeno;
    }
}

[Serializable, NetSerializable]
public sealed class ManageHiveReadmitConfirmEvent : EntityEventArgs
{
    public NetEntity Xeno;

    public ManageHiveReadmitConfirmEvent(NetEntity xeno)
    {
        Xeno = xeno;
    }
}
