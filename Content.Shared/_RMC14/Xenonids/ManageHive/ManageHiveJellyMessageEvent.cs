using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.ManageHive;

[Serializable, NetSerializable]
public sealed record ManageHiveJellyMessageEvent(NetEntity Xeno, string Name, string Message = "")
    : DialogInputEvent(Message);
