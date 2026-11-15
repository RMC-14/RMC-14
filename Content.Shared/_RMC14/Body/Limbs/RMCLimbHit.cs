using Content.Shared._RMC14.Body.Zones;

namespace Content.Shared._RMC14.Body.Limbs;

/// <summary>
///     how a piece of damage lands on a mob's limbs
/// </summary>
public readonly record struct RMCLimbHit(RMCLimbHitMode Mode, RMCTargetZone? Zone = null)
{
    // one random limb weighted by size, what anything that doesn't say gets
    public static readonly RMCLimbHit Random = new(RMCLimbHitMode.Limb);

    // split across limbs like cm13's overall damage, for fire heat cold and falling
    public static readonly RMCLimbHit Spread = new(RMCLimbHitMode.Spread);

    // half on one random limb and the rest spread, like cm13's explosions
    public static readonly RMCLimbHit Explosion = new(RMCLimbHitMode.Explosion);

    public static RMCLimbHit Aimed(RMCTargetZone zone)
    {
        return new RMCLimbHit(RMCLimbHitMode.Limb, zone);
    }
}

public enum RMCLimbHitMode : byte
{
    // one limb, the zone if there is one otherwise a random limb
    Limb,
    Spread,
    Explosion,
}
