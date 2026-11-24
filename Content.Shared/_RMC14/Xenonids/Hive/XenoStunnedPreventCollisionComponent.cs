using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hive;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoHiveSystem))]
public sealed partial class XenoStunnedPreventCollisionComponent : Component;
