using Content.Shared.Chat.Prototypes;
using Content.Shared._RMC14.Medical.Surgery;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedXenoParasiteSystem))]
public sealed partial class VictimInfectedComponent : Component
{
    /// <summary>
    ///     Textures for how progressed the larva is. Used by xenonid hud.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier[] InfectedIcons =
    [
        new Rsi(new ResPath("/Textures/_RMC14/Interface/xeno_hud.rsi"), "infected0"),
        new Rsi(new ResPath("/Textures/_RMC14/Interface/xeno_hud.rsi"), "infected1"),
        new Rsi(new ResPath("/Textures/_RMC14/Interface/xeno_hud.rsi"), "infected2"),
        new Rsi(new ResPath("/Textures/_RMC14/Interface/xeno_hud.rsi"), "infected3"),
        new Rsi(new ResPath("/Textures/_RMC14/Interface/xeno_hud.rsi"), "infected4"),
        new Rsi(new ResPath("/Textures/_RMC14/Interface/xeno_hud.rsi"), "infected5"),
        new Rsi(new ResPath("/Textures/_RMC14/Interface/xeno_hud.rsi"), "infected6")
    ];

    /// <summary>
    ///     The container ID of where the larva is stored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string LarvaContainerId = "rmc_larva_container";

    /// <summary>
    ///     The uid of the larva that is spawned.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SpawnedLarva;

    /// <summary>
    ///     How long it takes for the larva to burst out of the victim.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BurstDelay = TimeSpan.FromMinutes(8);

    /// <summary>
    ///     When the larva should be kicked out after the intial burst time.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AutoBurstTime = TimeSpan.FromSeconds(60);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan BurstAt;

    /// <summary>
    ///     How fast the larva incubates.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float IncubationMultiplier = 1;

    /// <summary>
    ///     The entity which is spawned during the infection process.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId BurstSpawn = "CMXenoLarva";

    [DataField, AutoNetworkedField]
    public SoundSpecifier BurstSound = new SoundCollectionSpecifier("XenoChestBurst");

    /// <summary>
    ///     Used by larva removal surgery.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(SharedCMSurgerySystem))]
    public bool RootsCut;

    /// <summary>
    ///     What hive the larva is from.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Hive;

    [DataField]
    public int FinalStage = 6;

    [DataField, AutoNetworkedField]
    public int CurrentStage = 0;

    [DataField]
    public int InitialSymptomsStart = 2;

    [DataField]
    public int MiddlingSymptomsStart = 3;

    [DataField]
    public int FinalSymptomsStart = 4;

    [DataField]
    public int BurstWarningStart = 6;

    [DataField]
    public float ShakesChance = 0.08f;

    [DataField]
    public float MinorPainChance = 0.03f;

    [DataField]
    public float ThroatPainChance = 0.015f;

    [DataField]
    public float MuscleAcheChance = 0.015f;

    [DataField]
    public float SneezeCoughChance = 0.015f;

    [DataField]
    public float MajorPainChance = 0.1f;

    [DataField]
    public float InsanePainChance = 0.15f;

    [DataField]
    public bool DidBurstWarning = false;

    [DataField, AutoNetworkedField]
    public bool IsBursting = false;

    [DataField]
    public TimeSpan BaseKnockdownTime = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan JitterTime = TimeSpan.FromSeconds(5);

    [DataField]
    public ProtoId<EmotePrototype> SneezeId = "Sneeze";

    [DataField]
    public ProtoId<EmotePrototype> CoughId = "Cough";

    [DataField]
    public ProtoId<EmotePrototype> ScreamId = "Scream";

    [DataField]
    public DamageSpecifier InfectionDamage = new() { DamageDict = new() { { "Blunt", 1 } } };

    /// <summary>
    ///     How long the do-after of the larva bursting takes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BurstDoAfterDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How long larva is invincible after bursting
    /// </summary>
    [DataField]
    public TimeSpan LarvaInvincibilityTime = TimeSpan.FromSeconds(1);
}
