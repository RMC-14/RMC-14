using Content.Shared.Damage;

namespace Content.Shared._RMC14.Damage;

[ByRefEvent]
public record struct DamageStateCritBeforeDamageEvent(DamageSpecifier Damage);
