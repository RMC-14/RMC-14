using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Brute;

[RegisterComponent]
public sealed partial class RMCBruteProjectileComponent : Component
{
    // M5510 wave timing and raw structure damage values.
    [DataField]
    public int MaxDistance = 7;

    [DataField]
    public float RowDelay = 0.1f;

    [DataField]
    public int StructureDamage = 1200;

    [DataField]
    public int EdgeLowerDamage = 400;

    [DataField]
    public int EdgeUpperDamage = 700;

    // Convert BRUTE breakage to local damage for RMC wall-like health scales.
    [DataField]
    public int WallDamageMultiplier = 15;

    [DataField]
    public float OpenDoorDamageMultiplier = 0.5f;

    [DataField]
    public int DoorDamageMultiplier = 15;

    [DataField]
    public float ResinExplosionDamageMultiplier = 0.85f;

    // These are independent per-tile rolls.
    [DataField]
    public float FireChance = 0.3f;

    [DataField]
    public float SparkChance = 0.3f;

    [DataField]
    public float SmokeChance = 0.3f;

    // Stores the inverse of the throw chance.
    [DataField]
    public float ThrowSkipChance = 0.2f;

    [DataField]
    public float LivingSizeSkipChance = 0.05f;

    [DataField]
    public float ThrowSpeed = 5f;

    [DataField]
    public EntProtoId FirePrototype = "RMCTileFireBrute";

    [DataField]
    public EntProtoId SparkPrototype = "RMCBruteSparks";

    [DataField]
    public EntProtoId SmokePrototype = "RMCBruteSmoke";

    public bool Primed;

    public Vector2 LastDirection = Vector2.UnitX;
}
