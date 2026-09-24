using Content.Shared.Damage;

namespace Content.Shared._RMC14.Medical.Defibrillator;

[ByRefEvent]
public record struct RMCDefibrillatorDamageModifyEvent(EntityUid Target, DamageSpecifier Heal);
