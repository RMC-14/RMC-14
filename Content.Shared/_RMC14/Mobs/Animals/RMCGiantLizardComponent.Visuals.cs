using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardComponent
{
    [DataField]
    public SoundSpecifier GrowlSound = new SoundCollectionSpecifier("RMCGiantLizardGrowl", AudioParams.Default.WithVolume(1));

    [DataField]
    public SoundSpecifier HissSound = new SoundCollectionSpecifier("RMCGiantLizardHiss", AudioParams.Default.WithVolume(-2));

    [DataField]
    public TimeSpan GrowlCooldownMin = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan GrowlCooldownMax = TimeSpan.FromSeconds(14);

    [ViewVariables]
    public TimeSpan NextGrowlAt;

    [DataField]
    public float TongueFlickChance = 0.25f;

    [DataField]
    public TimeSpan TongueFlickCooldown = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan TongueFlickDuration = TimeSpan.FromSeconds(0.3);

    [ViewVariables]
    public TimeSpan NextTongueFlickAt;

    [ViewVariables]
    public TimeSpan TongueFlickEndAt;

    [ViewVariables]
    public bool TongueVisible;

    [DataField]
    public float SmallWoundHealthFraction = 0.75f;

    [DataField]
    public float BigWoundHealthFraction = 0.5f;

    [DataField]
    public FixedPoint2 BleedTrailDamageThreshold = FixedPoint2.New(10);

    [DataField]
    public float BleedTrailDamageDivisor = 10f;

    [DataField]
    public int BleedTrailMaxTicks = 30;

    [DataField]
    public int BleedTrailSmallTicks = 10;

    [DataField]
    public TimeSpan BleedTrailCooldown = TimeSpan.FromSeconds(2);

    [DataField]
    public FixedPoint2 BleedTrailSmallVolume = FixedPoint2.New(1);

    [DataField]
    public FixedPoint2 BleedTrailLargeVolume = FixedPoint2.New(3);

    [ViewVariables]
    public int BleedTrailTicks;

    [ViewVariables]
    public TimeSpan NextBleedTrailAt;
}

[Serializable, NetSerializable]
public enum RMCGiantLizardVisualLayers : byte
{
    Base,
    Wounds,
    Tongue,
}

[Serializable, NetSerializable]
public enum RMCGiantLizardVisuals : byte
{
    Body,
    Wounds,
    Tongue,
}

[Serializable, NetSerializable]
public enum RMCGiantLizardBodyVisual : byte
{
    Running,
    Sleeping,
    KnockedDown,
    Dead,
}

[Serializable, NetSerializable]
public enum RMCGiantLizardWoundVisual : byte
{
    None,
    Small,
    Big,
    SmallRest,
    BigRest,
    SmallStun,
    BigStun,
}

public sealed partial class RMCGiantLizardPounceActionEvent : WorldTargetActionEvent;
