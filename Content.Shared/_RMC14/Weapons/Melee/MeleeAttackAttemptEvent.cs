using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Melee;

[ByRefEvent]
[Serializable, NetSerializable]
public record struct MeleeAttackAttemptEvent(NetEntity Target, AttackEvent Attack, NetCoordinates Coordinates , List<NetEntity> PotentialTargets,NetEntity? Weapon = null);
