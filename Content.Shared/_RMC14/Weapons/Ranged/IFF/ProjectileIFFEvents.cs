using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

/// <summary>
///     Raised after IFF data is assigned to a projectile.
///     Source is the IFF owner (usually the shooter or an intrinsic weapon), or an inherited projectile.
/// </summary>
[ByRefEvent]
public readonly record struct ProjectileIFFAddedEvent(EntityUid Source, EntityUid Projectile);

/// <summary>
///     Allows systems to ignore protection from one specific IFF faction without altering any other faction.
/// </summary>
[ByRefEvent]
public record struct ProjectileIFFCheckEvent(
    EntityUid Target,
    EntProtoId<IFFFactionComponent> Faction,
    bool IffEnabled,
    bool IgnoreProtection = false);
