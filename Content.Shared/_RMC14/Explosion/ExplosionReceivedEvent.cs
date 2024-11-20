using Content.Shared.Damage;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Explosion;

[ByRefEvent]
public readonly record struct ExplosionReceivedEvent(MapCoordinates Epicenter, DamageSpecifier Damage);
