using Robust.Shared.Audio;

namespace Content.Shared._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardComponent
{
    [DataField]
    public TimeSpan CalmRestDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan RestCheckCooldown = TimeSpan.FromSeconds(2);

    [DataField]
    public float RestChanceGainMin = 1f;

    [DataField]
    public float RestChanceGainMax = 2f;

    [DataField]
    public float FriendlyPetRestChanceBonus = 15f;

    [DataField]
    public float RestChanceMax = 100f;

    [ViewVariables]
    public float RestChance;

    [ViewVariables]
    public TimeSpan NextRestCheckAt;

    [DataField]
    public TimeSpan RestHealCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextRestHealAt;

    [DataField]
    public TimeSpan StatusRecoveryCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextStatusRecoveryAt;

    [ViewVariables]
    public bool Resting;

    [ViewVariables]
    public bool SleepingForRest;

    [DataField]
    public float FirePanicSpeed = 5f;

    [DataField]
    public TimeSpan FirePanicDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan FirePanicCooldown = TimeSpan.FromSeconds(1);

    [DataField]
    public int FireResistStacks = -10;

    [DataField]
    public TimeSpan FireResistStun = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextFirePanicAt;

    [DataField]
    public TimeSpan CalmEmoteCooldown = TimeSpan.FromSeconds(3);

    [ViewVariables]
    public TimeSpan NextCalmEmoteAt;

    [DataField]
    public float FriendlyPetHissChance = 0.5f;

    [DataField]
    public TimeSpan FriendlyPetEmoteCooldownMin = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan FriendlyPetEmoteCooldownMax = TimeSpan.FromSeconds(8);

    [ViewVariables]
    public TimeSpan NextFriendlyPetEmoteAt;

    [DataField]
    public float DisarmKnockdownChance = 0.25f;

    [DataField]
    public TimeSpan DisarmKnockdown = TimeSpan.FromSeconds(0.4);

    [DataField]
    public SoundSpecifier DisarmKnockdownSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/alien_knockdown.ogg", AudioParams.Default.WithVolume(-4));

    [ViewVariables]
    public bool SleepingForPossession;
}
