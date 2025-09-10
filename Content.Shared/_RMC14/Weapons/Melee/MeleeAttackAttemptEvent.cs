using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Melee;

/// <summary>
///     Raised on an entity before checking if it actually can attack.
/// </summary>
/// <param name="Target">The entity being targeted by the attack</param>
/// <param name="Attack">The <see cref="AttackEvent"/></param>
/// <param name="Coordinates">The coordinates being attacked</param>
/// <param name="PotentialTargets">Entities that might possibly be hit instead, or alongside, the primary target</param>
/// <param name="Weapon">The entity used to do the attack</param>
[ByRefEvent]
[Serializable, NetSerializable]
public record struct MeleeAttackAttemptEvent(NetEntity Target, AttackEvent Attack, NetCoordinates Coordinates, List<NetEntity> PotentialTargets, NetEntity Weapon);
