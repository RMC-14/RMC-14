
namespace Content.Shared._RMC14.Weapons.Ranged;

[ByRefEvent]
public record struct RMCTryAmmoEjectEvent(
    EntityUid User,
    bool Cancelled
);
