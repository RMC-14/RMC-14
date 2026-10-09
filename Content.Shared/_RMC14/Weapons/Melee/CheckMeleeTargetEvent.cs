using Content.Shared.Weapons.Melee;

namespace Content.Shared._RMC14.Weapons.Melee;

/// <summary>
/// Event raised on attacking entities to check if their melee target should be allowed. Allows them to decide if a target should be skipped entirely
/// or deferred so that other entities are prioritized to become targets.
/// Handling this event should cause <b>NO SIDE EFFECTS</b>.
/// </summary>
/// <remarks>This event is similar to AttackAttemptEvent, but unfortunately that event is used
/// to trigger side effects when something has already begun an attack. This event, on the other hand, should
/// be used to preclude an entity from receiving a melee attack in the first place.</remarks>
/// <param name="Skip">Set to true if the target should be skipped, disallowing it from being a target.</param>
/// <param name="Defer">Set to true if the target should be deferred, allowing it to be a target only if there are no non-deferred targets.</param>
[ByRefEvent]
public record struct CheckMeleeTargetEvent(
    in EntityUid Target,
    in MeleeWeaponComponent? Weapon,
    in bool Disarm,
    bool Skip = false,
    bool Defer = false);
