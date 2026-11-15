using Content.Shared._RMC14.Body.Zones;

namespace Content.Shared._RMC14.Body.Limbs;

/// <summary>
///     how a piece of damage lands on a mob's limbs
/// </summary>
public readonly record struct RMCLimbHit(RMCLimbHitMode Mode, RMCTargetZone? Zone = null, float SpreadChance = 0.8f, float AimChance = 1f)
{
    // one random limb weighted by size, what anything that doesn't say gets
    public static readonly RMCLimbHit Random = new(RMCLimbHitMode.Limb);

    // one random limb that isn't at max with every limb equally likely
    public static readonly RMCLimbHit AnyLimb = new(RMCLimbHitMode.AnyLimb);

    // split across limbs overall damage, each limb not at max has an 80% chance to take a share
    public static readonly RMCLimbHit Spread = new(RMCLimbHitMode.Spread);

    // spread but every limb not at max takes a share, uses it for falling
    public static readonly RMCLimbHit SpreadAll = new(RMCLimbHitMode.Spread, SpreadChance: 1f);

    // half on any one limb and the rest spread by explosion weight
    public static readonly RMCLimbHit Explosion = new(RMCLimbHitMode.Explosion);

    // lands on the zone by chance otherwise on a random other limb
    public static RMCLimbHit Aimed(RMCTargetZone zone, float chance = 1f)
    {
        return new RMCLimbHit(RMCLimbHitMode.Limb, zone, AimChance: chance);
    }
}

public enum RMCLimbHitMode : byte
{
    // one limb, the zone if there is one otherwise a random limb weighted by size
    Limb,

    // one limb picked evenly from the limbs not at max
    AnyLimb,
    Spread,
    Explosion,
}
