using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinHole;

[RegisterComponent]
public sealed partial class XenoResinHoleComponent : Component
{
    public const string ParasitePrototype = "CMXenoParasite";

    public const string AcidGasPrototype = "XenoBombardAcidProjectile";

    public const string NeuroGasPrototype = "XenoBombardNeurotoxinProjectile";

    /// <summary>
    /// The entity to spawn on the trap when activated
    /// </summary>
    public EntProtoId? TrapPrototype = null;

    [DataField]
    public TimeSpan StepStunDuration = TimeSpan.FromSeconds(2.5);

    [DataField]
    public TimeSpan AddParasiteDelay = TimeSpan.FromSeconds(3.0);

    [DataField]
    public TimeSpan AddFluidDelay = TimeSpan.FromSeconds(3.0);

    [DataField]
    public float ParasiteActivationRange = 0.5f;

    [DataField]
    public float FluidActivationRange = 1.5f;
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
    AcidSplash
}
