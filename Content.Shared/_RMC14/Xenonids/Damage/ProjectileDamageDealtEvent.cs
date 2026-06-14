using Content.Shared.Damage;

namespace Content.Shared._RMC14.Xenonids.Damage;

[ByRefEvent]
public record struct ProjectileDamageDealtEvent(EntityUid? Origin, DamageSpecifier? DamageDelta);
