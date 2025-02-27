using Content.Shared.Damage;

namespace Content.Shared._RMC14.Xenonids.Stab;

[ByRefEvent]
public record struct RMCGetTailStabBonusDamageEvent(DamageSpecifier Damage);
