using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinHole;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoResinHoleComponent : Component
{
    public const string ParasitePrototype = "CMXenoParasite";

    public const string BoilerAcid = "XenoBombardAcidProjectile";

    public const string AcidGasPrototype = "RMCSmokeAcid";

    public const string BoilerNeuro = "XenoBombardNeurotoxinProjectile";

    public const string NeuroGasPrototype = "RMCSmokeNeurotoxin";

    public const string AcidPrototype = "XenoAcidSprayTrap";

    public const string WeakAcidPrototype = "XenoAcidSprayTrapWeak";

    public const string StrongAcidPrototype = "XenoAcidSprayTrapStrong";

    /// <summary>
    /// The entity to spawn on the trap when activated
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? TrapPrototype = null;

    [DataField]
    public TimeSpan StepStunDuration = TimeSpan.FromSeconds(2.0);

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
    public Color MessageColor = Color.FromHex("#2A623D");

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
