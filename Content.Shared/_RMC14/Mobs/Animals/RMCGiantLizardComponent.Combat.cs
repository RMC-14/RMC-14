using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardComponent
{
    [DataField]
    public float MinPounceRange = 1f;

    [DataField]
    public float MaxPounceRange = 5f;

    [DataField]
    public TimeSpan PounceCooldown = TimeSpan.FromSeconds(9);

    [ViewVariables]
    public TimeSpan NextPounceAt;

    [DataField]
    public int PounceStrength = 20;

    [ViewVariables]
    public bool Leaping;

    [ViewVariables]
    public EntityCoordinates PounceOrigin;

    [ViewVariables]
    public TimeSpan PounceEndAt;

    [ViewVariables]
    public EntityUid? PounceTarget;

    [DataField]
    public DamageSpecifier PounceDamage = new()
    {
        DamageDict = { { "Blunt", FixedPoint2.New(8) } }
    };

    [DataField]
    public TimeSpan PounceKnockdown = TimeSpan.FromSeconds(2.5);

    [DataField]
    public TimeSpan PounceBlockedKnockdown = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan PounceObstacleKnockdown = TimeSpan.FromSeconds(1);

    [DataField]
    public float PounceBlockedKnockback = 1.25f;

    [DataField]
    public float PounceBlockedKnockbackSpeed = 10f;

    [DataField]
    public DamageSpecifier PounceObstacleDamage = new()
    {
        DamageDict = { { "Blunt", FixedPoint2.New(10) } }
    };

    [ViewVariables]
    public EntityUid? RavageTarget;

    [ViewVariables]
    public int RavageHitsLeft;

    [ViewVariables]
    public TimeSpan NextRavageAt;

    [DataField]
    public int RavageHitCount = 3;

    [DataField]
    public TimeSpan RavageHitDelay = TimeSpan.FromSeconds(0.4);

    [DataField]
    public TimeSpan RavageKnockdown = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan RavageDaze = TimeSpan.FromSeconds(1.5);

    [DataField]
    public int RavageCameraShakeStrength = 1;

    [DataField]
    public TimeSpan RavageCooldownRefund = TimeSpan.FromSeconds(3);

    [DataField]
    public DamageSpecifier RavageDamage = new()
    {
        DamageDict = { { "Slash", FixedPoint2.New(9) } }
    };

    [DataField]
    public DamageSpecifier XenoBonusDamage = new()
    {
        DamageDict = { { "Slash", FixedPoint2.New(8) } }
    };

    [DataField]
    public DamageSpecifier MeleeXenoBonusDamage = new()
    {
        DamageDict = { { "Slash", FixedPoint2.New(7) } }
    };

    [DataField]
    public SoundSpecifier BiteAttackSound = new SoundCollectionSpecifier("RMCGiantLizardBite", AudioParams.Default.WithVolume(-2));

    [DataField]
    public SoundSpecifier SlashAttackSound = new SoundCollectionSpecifier("RMCGiantLizardSlash", AudioParams.Default.WithVolume(-2));
}
