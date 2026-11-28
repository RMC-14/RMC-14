using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared._RMC14.Weapons.Ranged;

/// <summary>
/// Event raised on entities that might become targets for a gun attack. Allows them to decide if they should be skipped entirely
/// from becoming an attack target or deferred so that other entities are prioritized to become targets.
/// Handling this event should cause <b>NO SIDE EFFECTS</b>.
/// </summary>
/// <param name="Skip">Set to true if this entity should be skipped, disallowing it from being targetted by the attack.</param>
/// <param name="Defer">Set to true if this entity should be deferred, allowing it to be attacked but only if there are no non-deferred targets.</param>
[ByRefEvent]
public record struct CheckGunAttackerEvent(
    in EntityUid Attacker,
    in Entity<GunComponent> Gun,
    bool Skip = false,
    bool Defer = false);
