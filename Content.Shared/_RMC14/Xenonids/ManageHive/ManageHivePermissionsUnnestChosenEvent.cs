using Content.Shared._RMC14.Xenonids.Hive;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.ManageHive;

[Serializable, NetSerializable]
public record ManageHivePermissionsUnnestChosenEvent(XenoUnnestPermission Choice);
