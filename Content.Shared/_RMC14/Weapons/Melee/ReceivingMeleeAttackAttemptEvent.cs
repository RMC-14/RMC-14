using Content.Shared.Weapons.Melee;

namespace Content.Shared._RMC14.Weapons.Melee;

/// <summary>
/// Entities that shouldn't even be considered as targets for melee attacks should cancel this event.
/// Entities that can be targets but other targets should be prioritized should set Deferred to true.
/// </summary>
/// <remarks>This event is similar to GettingAttackedAttemptEvent, but unfortunately that event is used
/// to trigger side effects when something has already begun receiving an attack. This event, on the other hand, should
/// be used to preclude the entity from receiving a melee attack in the first place. As such, handling this event should
/// <b>NEVER</b> cause side effects.</remarks>
/// <param name="Cancelled">Will be set to true if the entity should NEVER be considered as a target for a melee attack.</param>
/// <param name="Deferred">Will be set to true if the entity CAN be considered as a melee attack target, but other targets should be prioritized.</param>
[ByRefEvent]
public record struct ReceivingMeleeAttackAttemptEvent(EntityUid Attacker, MeleeWeaponComponent? Weapon, bool Disarm, bool Cancelled = false, bool Deferred = false);
