using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared._RMC14.Dialog;

namespace Content.Shared._RMC14.Xenonids.Banish;

[Serializable, NetSerializable]
public sealed class ManageHiveBanishEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class ManageHiveBanishXenoEvent(NetEntity xeno) : EntityEventArgs
{
    public NetEntity Xeno = xeno;
}

[Serializable, NetSerializable]
public sealed record ManageHiveBanishReasonEvent(NetEntity Xeno, string Reason) : DialogInputEvent(Reason)
{
    public string GetReason() => Message;
}

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

[ByRefEvent]
public record struct XenoBanishedEvent(EntityUid Banisher, EntityUid Banished, string Reason);

[ByRefEvent]
public record struct XenoReadmittedEvent(EntityUid Readmitter, EntityUid Readmitted);
