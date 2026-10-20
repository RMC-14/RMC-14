using Content.Shared.Weapons.Melee;

namespace Content.Shared._RMC14.Weapons.Melee;

/// <summary>
/// Event raised on entities that might become targets for an attack. Allows them to decide if they should be skipped entirely
/// from becoming an attack target or deferred so that other entities are prioritized to become targets.
/// Handling this event should cause <b>NO SIDE EFFECTS</b>.
/// </summary>
/// <remarks>This event is similar to GettingAttackedAttemptEvent, but unfortunately that event is used
/// to trigger side effects when something has already begun receiving an attack. This event, on the other hand, should
/// be used to preclude the entity from receiving a melee attack in the first place.</remarks>
/// <param name="Skip">Set to true if this entity should be skipped, disallowing it from being targetted by the attack.</param>
/// <param name="Defer">Set to true if this entity should be deferred, allowing it to be attacked but only if there are no non-deferred targets.</param>
[ByRefEvent]
public record struct CheckMeleeAttackerEvent(
    in EntityUid Attacker,
    in MeleeWeaponComponent? Weapon,
    in bool Disarm,
    bool Skip = false,
    bool Defer = false);
