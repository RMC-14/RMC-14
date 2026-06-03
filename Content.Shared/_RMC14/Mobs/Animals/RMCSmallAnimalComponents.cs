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
public sealed partial class RMCTinyLizardComponent : Component
{
    [DataField]
    public float HissChance = 0.35f;

    [DataField]
    public SoundSpecifier HissSound = new SoundPathSpecifier("/Audio/Animals/snake_hiss.ogg");

    [DataField]
    public float ShooKnockback = 0.75f;

    [DataField]
    public float ShooKnockbackSpeed = 6f;

    [DataField]
    public TimeSpan StompPopupCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextStompPopupAt;
}

[RegisterComponent]
public sealed partial class RMCAlienSlugComponent : Component
{
    [DataField]
    public TimeSpan ThinkCooldown = TimeSpan.FromSeconds(3);

    [DataField]
    public float SleepChance = 0.005f;

    [DataField]
    public float WakeChance = 0.01f;

    [DataField]
    public float BlurpChance = 0.01f;

    [DataField]
    public float WiggleChance = 0.015f;

    [DataField]
    public TimeSpan SleepDurationMin = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan SleepDurationMax = TimeSpan.FromSeconds(60);

    [DataField]
    public TimeSpan EmoteCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public float ShooKnockback = 0.55f;

    [DataField]
    public float ShooKnockbackSpeed = 5f;

    [DataField]
    public TimeSpan StompPopupCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextThinkAt;

    [ViewVariables]
    public TimeSpan SleepUntil;

    [ViewVariables]
    public TimeSpan NextEmoteAt;

    [ViewVariables]
    public TimeSpan NextStompPopupAt;

    [ViewVariables]
    public bool Sleeping;
}

[RegisterComponent]
public sealed partial class RMCBunnyComponent : Component
{
    [DataField]
    public TimeSpan ThinkCooldown = TimeSpan.FromSeconds(4);

    [DataField]
    public float HeardEmoteChance = 0.01f;

    [DataField]
    public float SeenEmoteChance = 0.01f;

    [DataField]
    public float ShooKnockback = 0.45f;

    [DataField]
    public float ShooKnockbackSpeed = 4f;

    [DataField]
    public TimeSpan KickPopupCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextThinkAt;

    [ViewVariables]
    public TimeSpan NextKickPopupAt;
}
