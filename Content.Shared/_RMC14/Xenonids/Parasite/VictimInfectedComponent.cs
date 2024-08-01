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

    [DataField, AutoNetworkedField]
    public TimeSpan FallOffDelay = TimeSpan.FromSeconds(20);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan FallOffAt;

    [DataField, AutoNetworkedField]
    public bool FellOff;

    [DataField, AutoNetworkedField]
    public TimeSpan BurstDelay = TimeSpan.FromMinutes(8);

    [DataField, AutoNetworkedField]
    public TimeSpan AttachedAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan BurstAt;

    [DataField, AutoNetworkedField]
    public float IncubationMultiplier = 1;

    [DataField, AutoNetworkedField]
    public EntProtoId BurstSpawn = "CMXenoLarva";

    [DataField, AutoNetworkedField]
    public SoundSpecifier BurstSound = new SoundCollectionSpecifier("XenoChestBurst");

    [DataField, AutoNetworkedField, Access(typeof(SharedCMSurgerySystem))]
    public bool RootsCut;

    [DataField, AutoNetworkedField]
    public EntityUid? Hive;

    [DataField]
    public int FinalStage = 5;

    [DataField, AutoNetworkedField]
    public int CurrentStage = 0;

    [DataField]
    public int InitialSymptomsStart = 2;

    [DataField]
    public int MiddlingSymptomsStart = 3;

    [DataField]
    public int FinalSymptomsStart = 4;

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
    public bool DidBurstWarning = false;

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
}
