using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Jittering;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(RMCJitterinessSystem))]
public sealed partial class RMCJitterinessComponent : Component
{
    [DataField]
    public float Jitteriness;

    [DataField]
    public float Max = 1000f;

    [DataField]
    public float VisibleThreshold = 100f;

    [DataField]
    public float Decay = 3f;

    [DataField]
    public float RestingDecay = 15f;

    [DataField]
    public TimeSpan DecayEvery = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDecay;
}
