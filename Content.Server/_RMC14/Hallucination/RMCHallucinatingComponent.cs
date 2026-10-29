using System.Numerics;
using Content.Shared._RMC14.Hallucination;
using Content.Shared.Alert;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RMC14.Hallucination;

[RegisterComponent]
[Access(typeof(RMCHallucinationSystem))]
public sealed partial class RMCHallucinatingComponent : Component
{
    [DataField]
    public TimeSpan TickEvery = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    [DataField]
    public TimeSpan? NextEvent;

    [DataField]
    public TimeSpan? NextSound;

    [DataField]
    public float EventThreshold = 1;

    [DataField]
    public TimeSpan EventDelayMin = TimeSpan.FromSeconds(8);

    [DataField]
    public TimeSpan EventDelayMax = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan EventDelayFloor = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan EventDelayCeiling = TimeSpan.FromSeconds(30);

    [DataField]
    public float EventDelayScale = 25;

    [DataField]
    public float EventDelayExponent = 1.25f;

    [DataField]
    public int RecentEventMemory = 4;

    [DataField]
    public float RepeatWeightMultiplier = 0.25f;

    [DataField]
    public List<EntProtoId> RecentEvents = new();

    [DataField]
    public RMCFakeHud FakeHud;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan FakeHudEnd;

    [DataField]
    public bool ShowingFakeOxygen;

    [DataField]
    public ProtoId<AlertPrototype> CriticalAlert = "HumanCrit";

    [DataField]
    public ProtoId<AlertPrototype> DeadAlert = "HumanDead";

    [DataField]
    public ProtoId<AlertPrototype> OxygenAlert = "LowOxygen";

    [DataField]
    public ProtoId<AlertPrototype> ToxinAlert = "Toxins";

    [DataField]
    public bool ShowingFakeToxin;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ItemEnd;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ImageEnd;

    [DataField]
    public float ViewRange = 7;

    [DataField]
    public float BodyCloneRange = 15;

    [DataField]
    public int MaxFakeBodies = 4;

    [DataField]
    public TimeSpan BodySpeakMin = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan BodySpeakMax = TimeSpan.FromSeconds(9);

    [DataField]
    public float BodyBegChance = 0.6f;

    [DataField]
    public List<RMCHallucinationBody> FakeBodies = new();

    [DataField]
    public TimeSpan AttackerSpeakMin = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan AttackerSpeakMax = TimeSpan.FromSeconds(10);

    [DataField]
    public float AttackerSpeakRange = 7;

    [DataField]
    public TimeSpan ParasiteKnockdown = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan ParasiteFaceDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan ParasiteFaceFade = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan ParasiteGiveUp = TimeSpan.FromSeconds(8);

    public RMCHallucinationParasiteChase? Parasite;

    [DataField]
    public EntityUid? ParasiteGrid;

    [DataField]
    public SoundSpecifier QueenStepSound = new SoundCollectionSpecifier("XenoFootstepLarge");

    [DataField]
    public TimeSpan QueenStepEvery = TimeSpan.FromSeconds(0.6);

    [DataField]
    public float QueenStartDistance = 12;

    [DataField]
    public SoundSpecifier QueenGrowlSound = new SoundCollectionSpecifier("XenoGrowl");

    [DataField]
    public TimeSpan? NextQueenStep;

    [DataField]
    public EntityUid? QueenGrid;

    [DataField]
    public Vector2 QueenPosition;

    [DataField]
    public int QueenStepsLeft;

    [DataField]
    public List<RMCHallucinationSound> Sounds = new()
    {
        new(new SoundCollectionSpecifier("RMCHallucinationExplosion")),
        new(new SoundPathSpecifier("/Audio/_RMC14/Hallucinations/Glassbr1.ogg")),
        new(new SoundPathSpecifier("/Audio/_RMC14/Hallucinations/Glassbr2.ogg")),
        new(new SoundPathSpecifier("/Audio/_RMC14/Hallucinations/Glassbr3.ogg")),
        new(new SoundPathSpecifier("/Audio/_RMC14/Hallucinations/twobeep.ogg")),
        new(new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/Gunshots/Gunshot.ogg"), true),
        new(new SoundCollectionSpecifier("RMCHallucinationCreepy")),
    };

    [DataField]
    public float SoundDistanceMin = 3;

    [DataField]
    public float SoundDistanceMax = 7;

    [DataField]
    public TimeSpan SecondSoundMin = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan SecondSoundMax = TimeSpan.FromSeconds(3);

    [DataField]
    public SoundSpecifier WeaponHitSound = new SoundCollectionSpecifier("RMCHallucinationWeaponHit");

    [DataField]
    public SoundSpecifier PunchSound = new SoundCollectionSpecifier("RMCHallucinationPunch");

    [DataField]
    public SoundSpecifier GrenadeSound = new SoundPathSpecifier("/Audio/_RMC14/Explosion/armbomb.ogg");

    [DataField]
    public float GrenadeSpinSpeed = 15;

    [DataField]
    public SoundSpecifier SkitterSound = new SoundCollectionSpecifier("XenoVentCrawl");

    [DataField]
    public SoundSpecifier ExplosionSound = new SoundCollectionSpecifier("CMExplosion", AudioParams.Default.WithVolume(-3));

    [DataField]
    public SoundSpecifier C4BeepSound = new SoundPathSpecifier("/Audio/Effects/beep1.ogg");

    [DataField]
    public TimeSpan C4BeepEvery = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan FlashDurationMin = TimeSpan.FromSeconds(8);

    [DataField]
    public TimeSpan FlashDurationMax = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan BodyDurationMin = TimeSpan.FromSeconds(30);

    [DataField]
    public TimeSpan BodyDurationMax = TimeSpan.FromSeconds(60);

    [DataField]
    public SoundSpecifier BigExplosionSound = new SoundCollectionSpecifier("RMCExplosionBig");

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan StrikeEnd;

    [DataField]
    public SoundSpecifier OrbitalAlertSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/ob_alert.ogg");

    [DataField]
    public SoundSpecifier OrbitalTravelSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/gun_orbital_travel.ogg", AudioParams.Default.WithMaxDistance(75));

    [DataField]
    public List<EntProtoId> OrbitalWarheads = new()
    {
        "RMCOrbitalCannonWarheadExplosive",
        "RMCOrbitalCannonWarheadIncendiary",
        "RMCOrbitalCannonWarheadCluster",
    };

    [DataField]
    public SoundSpecifier MortarTravelSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/gun_mortar_travel.ogg");

    [DataField]
    public SoundSpecifier CasFireSound = new SoundPathSpecifier("/Audio/_RMC14/Dropship/gau.ogg");

    [DataField]
    public SoundSpecifier CasImpactSound = new SoundPathSpecifier("/Audio/_RMC14/Dropship/gauimpact.ogg", AudioParams.Default.WithVolume(-6));

    [DataField]
    public SoundSpecifier DoorPrySound = new SoundCollectionSpecifier("XenoPry");

    [DataField]
    public SoundSpecifier HissSound = new SoundCollectionSpecifier("XenoHiss");

    [DataField]
    public SoundSpecifier ChestburstSound = new SoundCollectionSpecifier("XenoChestBurst");

    [DataField]
    public TimeSpan LurkerDuration = TimeSpan.FromSeconds(20);

    [DataField]
    public float LurkerVanishRange = 4;

    [DataField]
    public float LurkerHoverRange = 0.75f;

    [DataField]
    public TimeSpan LurkerFade = TimeSpan.FromSeconds(0.3);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LurkerEnd;

    [DataField]
    public EntProtoId WeedsNodePrototype = "XenoWeedsSource";

    [DataField]
    public EntProtoId WeedsPrototype = "XenoWeeds";

    [DataField]
    public SoundSpecifier WeedsPlantSound = new SoundCollectionSpecifier("RMCResinBuild", AudioParams.Default.WithVolume(-10f));

    [DataField]
    public TimeSpan WeedsLinger = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan WeedsFade = TimeSpan.FromSeconds(3);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan WeedsEnd;

    [DataField]
    public TimeSpan LightsDurationMin = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan LightsDurationMax = TimeSpan.FromSeconds(10);

    [DataField]
    public float LightsHissChance = 0.6f;

    [DataField]
    public TimeSpan LightsOnMin = TimeSpan.FromSeconds(0.1);

    [DataField]
    public TimeSpan LightsOnMax = TimeSpan.FromSeconds(0.6);

    [DataField]
    public TimeSpan LightsOffMin = TimeSpan.FromSeconds(0.05);

    [DataField]
    public TimeSpan LightsOffMax = TimeSpan.FromSeconds(0.2);

    [DataField]
    public float LightsBrownoutDepth = 0.6f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LightsEnd;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan FakeBlipsEnd;

    [DataField]
    public TimeSpan DisguiseDuration = TimeSpan.FromMinutes(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan DisguiseEnd;

    [DataField]
    public TimeSpan FakeInfectionDuration = TimeSpan.FromSeconds(60);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan FakeInfectionStart;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan FakeInfectionEnd;

    [DataField]
    public TimeSpan? NextFakeSymptom;

    [DataField]
    public List<Vector2> FakeBlips = new();

    [DataField]
    public List<RMCHallucinationStep> Steps = new();

    [DataField]
    public List<RMCHallucinationAttacker> Attackers = new();

    [DataField]
    public List<RMCHallucinationFakeBlood> FakeBlood = new();

    [DataField]
    public int NextAttackerId;

    [DataField]
    public int NextBodyId;
}

public enum RMCFakeHud : byte
{
    None,
    Critical,
    Dead,
    Oxygen,
    Toxin,
}

[DataDefinition]
public sealed partial class RMCHallucinationSound
{
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [DataField]
    public bool Twice;

    public RMCHallucinationSound()
    {
    }

    public RMCHallucinationSound(SoundSpecifier sound, bool twice = false)
    {
        Sound = sound;
        Twice = twice;
    }
}

[DataDefinition]
public sealed partial class RMCHallucinationAttacker
{
    [DataField]
    public int Id;

    [DataField]
    public EntityUid Clone;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public string? Weapon;

    [DataField]
    public EntityUid? WeaponEntity;

    [DataField]
    public EntProtoId? Animation;

    [DataField]
    public SoundSpecifier? FootstepSound;

    [DataField]
    public bool HasShoes;

    [DataField]
    public float StepDistance = 1.5f;

    [DataField]
    public float StepProgress;

    [DataField]
    public float FootstepVariation;

    [DataField]
    public EntityUid Grid;

    [DataField]
    public Vector2 Position;

    [DataField]
    public Vector2 Velocity;

    [DataField]
    public Direction Direction = Direction.South;

    [DataField]
    public bool VictimInside;

    [DataField]
    public float Health = 100;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan End;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextProcess;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSync;

    [DataField]
    public TimeSpan? BackOffUntil;

    [DataField]
    public bool SkippedLastTurn;

    [DataField]
    public TimeSpan? RemoveAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSpeak;
}

[DataDefinition]
public sealed partial class RMCHallucinationBody
{
    [DataField]
    public int Id;

    [DataField]
    public EntityUid Clone;

    [DataField]
    public EntityCoordinates Coordinates;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan End;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSpeak;
}

[DataDefinition]
public sealed partial class RMCHallucinationStep
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan At;

    [DataField]
    public EntityCoordinates? Coordinates;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public bool Explosion;

    [DataField]
    public LocId? Warning;

    [DataField]
    public LocId? WarningAbove;

    [DataField]
    public bool WarningChat;

    [DataField]
    public string? Message;

    [DataField]
    public EntProtoId? Visual;

    [DataField]
    public TimeSpan VisualDuration;
}

[DataRecord]
public record struct RMCHallucinationFakeBlood(
    EntityUid Grid,
    Vector2i Tile,
    [field: DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    TimeSpan End
);
