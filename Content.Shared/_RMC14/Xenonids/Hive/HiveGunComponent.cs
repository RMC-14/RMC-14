using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hive;

/// <summary>
/// Projectiles shot from this get autoassigned a hive from the weapon's hivemember
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoHiveSystem))]
public sealed partial class HiveGunComponent : Component;
