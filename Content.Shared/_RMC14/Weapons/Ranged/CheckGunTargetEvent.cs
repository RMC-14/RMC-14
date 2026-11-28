using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared._RMC14.Weapons.Ranged;

/// <summary>
/// Event raised on attacking entities to check if their gun target should be allowed. Allows them to decide if a target should be skipped entirely
/// or deferred so that other entities are prioritized to become targets.
/// Handling this event should cause <b>NO SIDE EFFECTS</b>.
/// </summary>
/// <param name="Skip">Set to true if the target should be skipped, disallowing it from being a target.</param>
/// <param name="Defer">Set to true if the target should be deferred, allowing it to be a target only if there are no non-deferred targets.</param>
[ByRefEvent]
public record struct CheckGunTargetEvent(
    in EntityUid Target,
    in Entity<GunComponent> Gun,
    bool Skip = false,
    bool Defer = false);
