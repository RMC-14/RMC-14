using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Dizziness;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(RMCDizzinessSystem))]
public sealed partial class RMCDizzinessComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Dizziness;

    [DataField, AutoNetworkedField]
    public bool Dizzy;

    [DataField]
    public float Max = 500f;

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

    [DataField]
    public float UnbearableChance = 0.01f;

    [DataField]
    public TimeSpan UnbearableEvery = TimeSpan.FromSeconds(0.1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUnbearable;
}
