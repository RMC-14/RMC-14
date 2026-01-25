using Content.Shared._RMC14.OrbitalCannon;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Neurotoxin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NeurotoxinComponent : Component
{
    [DataField, AutoNetworkedField]
    public float NeurotoxinAmount = 0;

    [DataField, AutoNetworkedField]
    public float DepletionPerTick = 1;

    [DataField, AutoNetworkedField]
    public float StaminaDamagePerTick = 7;

    [DataField, AutoNetworkedField]
    public TimeSpan DizzyStrength = TimeSpan.FromSeconds(12);

    [DataField, AutoNetworkedField]
    public TimeSpan DizzyStrengthOnStumble = TimeSpan.FromSeconds(55);

    [DataField, AutoNetworkedField]
    public TimeSpan LastMessage;

    [DataField, AutoNetworkedField]
    public TimeSpan TimeBetweenMessages = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan AccentTime = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public TimeSpan JitterTime = TimeSpan.FromSeconds(15);

    [DataField, AutoNetworkedField]
    public TimeSpan StumbleJitterTime = TimeSpan.FromSeconds(25);

    [DataField, AutoNetworkedField]
    public TimeSpan LastStumbleTime;

    [DataField, AutoNetworkedField]
    public TimeSpan NextHallucination;

    [DataField, AutoNetworkedField]
    public TimeSpan HallucinationEveryMin = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public TimeSpan HallucinationEveryMax = TimeSpan.FromSeconds(11);

    [DataField, AutoNetworkedField]
    public TimeSpan BlurTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan BlindTime = TimeSpan.FromSeconds(2); //0.5 seconds in parity but acts like 1 to stop it from fading in/out

    [DataField, AutoNetworkedField]
    public TimeSpan DeafenTime = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan MinimumDelayBetweenEvents = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan LastAccentTime;

    [DataField, AutoNetworkedField]
    public DamageSpecifier ToxinDamage = new();

    [DataField, AutoNetworkedField]
    public DamageSpecifier OxygenDamage = new();

    [DataField, AutoNetworkedField]
    public DamageSpecifier CoughDamage = new();

    [DataField, AutoNetworkedField]
    public TimeSpan DazeLength = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> CoughId = "Cough";

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> PainId = "Scream"; // TODO custom pain emote

    [DataField, AutoNetworkedField]
    public TimeSpan BloodCoughDuration = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan NextGasInjectionAt;

    [DataField, AutoNetworkedField]
    public TimeSpan NextNeuroEffectAt;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateEvery = TimeSpan.FromSeconds(1);

    [DataField]
    public Dictionary<NeuroHallucinations, float> Hallucinations = new()
    {
        {NeuroHallucinations.AlienAttack, 0.05f},
        {NeuroHallucinations.OB, 0.05f},
        {NeuroHallucinations.Screech, 0.06f},
        {NeuroHallucinations.CAS, 0.08f},
        {NeuroHallucinations.Mortar, 0.18f},
        {NeuroHallucinations.Giggle, 0.27f},
        {NeuroHallucinations.Sounds, 0.31f}
    };

    [DataField]
    public List<SoundSpecifier> HallucinationRandomSounds = new()
    {
        new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_distantroar_3.ogg"),
        new SoundPathSpecifier("/Audio/_RMC14/Xeno/xenos_roaring.ogg"),
        // new SoundCollectionSpecifier("XenoQueenBreath"), TODO RMC14 Queen Breath
        new SoundCollectionSpecifier("XenoRoar"),
        new SoundPathSpecifier("/Audio/_RMC14/Announcements/Marine/notice2.ogg"),
        new SoundPathSpecifier("/Audio/_RMC14/Weapons/alien_knockdown.ogg"), //TODO RMC14 Bonebreak sound
        new SoundCollectionSpecifier("CMM54CShoot"),
        new SoundCollectionSpecifier("MetalThud"),
        new SoundPathSpecifier("/Audio/Items/crowbar.ogg"),
        new SoundCollectionSpecifier("WindowShatter")
    };

    [DataField]
    public ProtoId<EmotePrototype> GiggleId = "Laugh";

    [DataField]
    public TimeSpan RainbowDuration = TimeSpan.FromSeconds(6);

    [DataField]
    public SoundSpecifier Screech = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_queen_screech.ogg", AudioParams.Default.WithVolume(-7));

    [DataField]
    public TimeSpan ScreechDownTime = TimeSpan.FromSeconds(2);

    [DataField]
    public SoundSpecifier Pounce = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_pounce.ogg");

    [DataField]
    public TimeSpan PounceDownTime = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier OBAlert = new SoundPathSpecifier("/Audio/_RMC14/Effects/ob_alert.ogg");

    [DataField]
    public SoundSpecifier FiremissionStart = new SoundPathSpecifier("/Audio/_RMC14/Weapons/dropship_sonic_boom.ogg");

    [DataField]
    public EntProtoId<OrbitalCannonWarheadComponent>[] WarheadTypes =
    ["RMCOrbitalCannonWarheadExplosive", "RMCOrbitalCannonWarheadIncendiary", "RMCOrbitalCannonWarheadCluster"];
}

[Serializable, NetSerializable]
public enum NeuroHallucinations
{
    AlienAttack,
    OB,
    Screech,
    CAS,
    Mortar,
    Giggle,
    Sounds
}
