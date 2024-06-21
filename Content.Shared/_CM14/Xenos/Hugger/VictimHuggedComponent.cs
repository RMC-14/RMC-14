using Content.Shared.Chat.Prototypes;
using Content.Shared._CM14.Medical.Surgery;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;
using Content.Shared.Damage;

namespace Content.Shared._CM14.Xenos.Hugger;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedXenoHuggerSystem))]
public sealed partial class VictimHuggedComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "cm_hugger_container";

    [DataField, AutoNetworkedField]
    public SpriteSpecifier HuggedSprite = new Rsi(new ResPath("/Textures/_CM14/Mobs/Xenos/Hugger/hugger_mask.rsi"), "human");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier[] HuggedIcons =
    [
        new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_hud.rsi"), "infected0"),
        new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_hud.rsi"), "infected1"),
        new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_hud.rsi"), "infected2"),
        new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_hud.rsi"), "infected3"),
        new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_hud.rsi"), "infected4"),
        new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_hud.rsi"), "infected5"),
        new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_hud.rsi"), "infected6")
    ];

    [DataField, AutoNetworkedField]
    public Enum HuggedLayer = VictimHuggedLayer.Hugged;

    [DataField, AutoNetworkedField]
    public TimeSpan FallOffDelay = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan FallOffAt;

    [DataField, AutoNetworkedField]
    public bool FellOff;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan RecoverAt;

    [DataField, AutoNetworkedField]
    public bool Recovered;

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

    [DataField, AutoNetworkedField]
    public int FinalStage = 5;

    [DataField, AutoNetworkedField]
    public int CurrentStage = 0;

    [DataField, AutoNetworkedField]
    public int InitialSymptomsStart = 2;

    [DataField, AutoNetworkedField]
    public int MiddlingSymptomsStart = 3;

    [DataField, AutoNetworkedField]
    public int FinalSymptomsStart = 4;

    [DataField, AutoNetworkedField]
    public float ShakesChance = 0.03f;

    [DataField, AutoNetworkedField]
    public float MinorPainChance = 0.02f;

    [DataField, AutoNetworkedField]
    public float ThroatPainChance = 0.01f;

    [DataField, AutoNetworkedField]
    public float MuscleAcheChance = 0.005f;

    [DataField, AutoNetworkedField]
    public float SneezeCoughChance = 0.01f;

    [DataField, AutoNetworkedField]
    public float MajorPainChance = 0.01f;

    [DataField, AutoNetworkedField]
    public TimeSpan BaseKnockdownTime = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan JitterTime = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> SneezeId = "Sneeze";

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> CoughId = "Cough";

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> ScreamId = "Scream";

    [DataField, AutoNetworkedField]
    public DamageSpecifier InfectionDamage = new() { DamageDict = new() { { "Blunt", 1 } } };
}
