using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.ManageHive;

[ByRefEvent]
[Serializable, NetSerializable]
public sealed record ManageHiveJellyNameEvent(NetEntity Xeno, string Name);
