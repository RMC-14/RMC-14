using Content.Shared.Damage;
using Content.Shared.Explosion;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Explosion;

[ByRefEvent]
public readonly record struct ExplosionReceivedEvent(ProtoId<ExplosionPrototype> Explosion, MapCoordinates Epicenter, DamageSpecifier Damage);
