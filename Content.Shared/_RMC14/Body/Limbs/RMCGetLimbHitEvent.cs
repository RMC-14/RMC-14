using Content.Shared.Damage;

namespace Content.Shared._RMC14.Body.Limbs;

/// <summary>
///     raised on whatever is dealing the damage, a projectile a weapon or the attacker's own body for unarmed hits
///     set hit to say where it lands, leave it null to let the limbs pick a random one
/// </summary>
[ByRefEvent]
public record struct RMCGetLimbHitEvent(EntityUid Target, DamageSpecifier Damage, RMCLimbHit? Hit = null);
