using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Mobs.Animals;

[RegisterComponent]
public sealed partial class RMCCatHunterComponent : Component
{
    [DataField]
    public float SearchRange = 7f;

    [DataField]
    public float AttackRange = 1.2f;

    [DataField]
    public TimeSpan ThinkCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextThinkAt;

    [DataField]
    public float MoveSpeed = 3f;

    [DataField]
    public int MaxPlayAttacks = 5;

    [ViewVariables]
    public int PlayCounter;

    [ViewVariables]
    public EntityUid? MovementTarget;

    [DataField]
    public TimeSpan PlayBreakCooldown = TimeSpan.FromSeconds(4);

    [DataField]
    public float ThreatenRange = 3f;

    [DataField]
    public float ThreatenChance = 0.15f;

    [DataField]
    public TimeSpan ThreatenCooldown = TimeSpan.FromSeconds(3);

    [ViewVariables]
    public TimeSpan NextThreatenAt;

    [DataField]
    public TimeSpan MeowCooldownMin = TimeSpan.FromSeconds(40);

    [DataField]
    public TimeSpan MeowCooldownMax = TimeSpan.FromSeconds(60);

    [ViewVariables]
    public TimeSpan NextMeowAt;

    [DataField]
    public TimeSpan AmbientEmoteCooldown = TimeSpan.FromSeconds(6);

    [DataField]
    public float HeardEmoteChance = 0.02f;

    [DataField]
    public float SeenEmoteChance = 0.02f;

    [ViewVariables]
    public TimeSpan NextAmbientEmoteAt;

    [DataField]
    public SoundSpecifier MeowSound = new SoundPathSpecifier("/Audio/Animals/cat_meow.ogg", AudioParams.Default.WithVolume(-4));

    [DataField]
    public SoundSpecifier HuntHitSound = new SoundCollectionSpecifier("AlienClaw", AudioParams.Default.WithVolume(-5));

    [DataField]
    public TimeSpan PlayerPreyKnockdown = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan PlayerPreySlowdown = TimeSpan.FromSeconds(4);

    [DataField]
    public DamageSpecifier PlayerPreyDamage = new()
    {
        DamageDict = { { "Blunt", FixedPoint2.New(25) } }
    };

    [DataField]
    public DamageSpecifier NpcPreyDamage = new()
    {
        DamageDict = { { "Blunt", FixedPoint2.New(200) } }
    };

    [DataField]
    public EntityWhitelist? PreyWhitelist;
}
