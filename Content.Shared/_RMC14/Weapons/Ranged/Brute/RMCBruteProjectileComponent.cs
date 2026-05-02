using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Brute;

[RegisterComponent]
public sealed partial class RMCBruteProjectileComponent : Component
{
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

    [DataField]
    public float FireChance = 0.3f;

    [DataField]
    public float SparkChance = 0.3f;

    [DataField]
    public float SmokeChance = 0.3f;

    [DataField]
    public float ThrowSkipChance = 0.2f;

    [DataField]
    public float LivingSizeSkipChance = 0.05f;

    [DataField]
    public float ThrowSpeed = 5f;

    [DataField]
    public EntProtoId FirePrototype = "RMCTileFire";

    [DataField]
    public EntProtoId SparkPrototype = "RMCBruteSparks";

    [DataField]
    public EntProtoId SmokePrototype = "RMCBruteSmoke";

    public bool Primed;

    public Vector2 LastDirection = Vector2.UnitX;
}
