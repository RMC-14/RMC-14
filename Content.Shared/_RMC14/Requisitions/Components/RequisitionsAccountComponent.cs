using Content.Shared._RMC14.Scaling;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Requisitions.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedRequisitionsSystem))]
public sealed partial class RequisitionsAccountComponent : Component
{
    [DataField]
    [Access(typeof(SharedRequisitionsSystem), typeof(ScalingSystem))]
    public bool Started;

    [DataField]
    [Access(typeof(SharedRequisitionsSystem), typeof(ScalingSystem))]
    public int Balance;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextGain;

    [DataField]
    public TimeSpan GainEvery = TimeSpan.FromSeconds(30);

    [DataField]
    public List<RequisitionsRandomCrates> RandomCrates = new();
}
