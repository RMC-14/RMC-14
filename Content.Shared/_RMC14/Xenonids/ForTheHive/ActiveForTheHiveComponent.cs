using Content.Shared.FixedPoint;
using Content.Shared.Damage;
using Content.Shared._RMC14.Xenonids.Acid;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.ForTheHive;

[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveForTheHiveComponent : Component
{
    [DataField]
    public TimeSpan Duration;

    [DataField]
    public TimeSpan TimeLeft;

    [DataField]
    public SoundSpecifier WindingUpSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/runner_charging_1.ogg");

    [DataField]
    public SoundSpecifier WindingDownSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/runner_charging_2.ogg");

    [DataField]
    public TimeSpan NextUpdate;

    [DataField]
    public TimeSpan UpdateEvery = TimeSpan.FromSeconds(1);

    [DataField]
    public bool UseWindUpSound = true;

    [DataField]
    public float InitialVolume = -3f;

    [DataField]
    public float MaxVolume = 23f;

    [DataField]
    public DamageSpecifier BaseDamage = new();

    [DataField]
    public float AcidRangeRatio = 200f;

    [DataField]
    public float BurnRangeRatio = 100f;

    [DataField]
    public float BurnDamageRatio = 5f;

    [DataField]
    public EntProtoId Acid = "XenoAcidNormal";

    [DataField]
    public XenoAcidStrength AcidStrength = XenoAcidStrength.Normal;

    [DataField]
    public TimeSpan AcidTime = TimeSpan.FromSeconds(255);

    [DataField]
    public float AcidDps = 8;

    [DataField]
    public EntProtoId AcidSmoke = "RMCSmokeRunner";

    [DataField]
    public SoundSpecifier KaboomSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/blobattack.ogg");

    [DataField]
    public TimeSpan CoreSpawnTime = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan CorpseSpawnTime = TimeSpan.FromSeconds(0.5);

    [DataField]//From Delays
    public FixedPoint2 SlowDown = FixedPoint2.New(0.45);

    [DataField]
    public ComponentRegistry? MobAcid;
}


[Serializable, NetSerializable]
public enum ForTheHiveVisuals : byte
{
    Time,
}

[Serializable, NetSerializable]
public enum ForTheHiveVisualLayers : byte
{
    Base,
}
