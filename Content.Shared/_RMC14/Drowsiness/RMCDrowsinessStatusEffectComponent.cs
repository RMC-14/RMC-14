using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Drowsiness;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RMCDrowsinessStatusEffectComponent : Component
{
    [DataField]
    public TimeSpan Threshold = TimeSpan.FromSeconds(10);

    [DataField]
    public float Chance = 0.05f;

    [DataField]
    public TimeSpan CheckEvery = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan Sleep = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan KnockOut = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextCheck;
}
