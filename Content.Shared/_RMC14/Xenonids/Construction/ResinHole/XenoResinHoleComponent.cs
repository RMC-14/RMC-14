using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinHole;

[RegisterComponent]
public sealed partial class XenoResinHoleComponent : Component
{
    public const string ParasitePrototype = "CMXenoParasite";

    public const string AcidGasPrototype = "XenoBombardAcidProjectile";

    public const string NeuroGasPrototype = "XenoBombardNeurotoxinProjectile";

    public const string AcidPrototype = "XenoAcidSprayTrap";

    public const string WeakAcidPrototype = "XenoAcidSprayTrapWeak";

    public const string StrongAcidPrototype = "XenoAcidSprayTrapStrong";

	/// <summary>
	/// The entity to spawn on the trap when activated
	/// </summary>
	[DataField]
	public EntProtoId? TrapPrototype = null;

	/// <summary>
	/// The hive that will get announcements when the hole is broken or activated
	/// </summary>
	[DataField]
	public EntityUid? Hive = null;

    [DataField]
    public TimeSpan StepStunDuration = TimeSpan.FromSeconds(2.5);

    [DataField]
    public TimeSpan AddParasiteDelay = TimeSpan.FromSeconds(3.0);

    [DataField]
    public TimeSpan AddFluidDelay = TimeSpan.FromSeconds(3.0);

    [DataField]
    public float ParasiteActivationRange = 0.5f;

//    [DataField] used but emulated through step trigger range being very low
//    public float FluidActivationRange = 1.5f;

    [DataField]
    public SoundSpecifier? FluidFillSound = new SoundPathSpecifier("/Audio/Effects/refill.ogg");

    [DataField]
    public SoundSpecifier BuildSound = new SoundCollectionSpecifier("RMCResinBuild")
    {
        Params = AudioParams.Default.WithVolume(-5f)
    };
}

[Serializable, NetSerializable]
public enum XenoResinHoleLayers
{
    Base
}

[Serializable, NetSerializable]
public enum XenoResinHoleVisuals
{
    Contained
}

[Serializable, NetSerializable]
public enum ContainedTrap
{
    Empty,
    Parasite,
    NeuroticGas,
    AcidGas,
    Acid1,
    Acid2,
    Acid3
}
