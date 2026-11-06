using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Construction.BuildMark;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(XenoBuildMarkSystem))]
public sealed partial class XenoBuildMarkerComponent : Component
{
    [DataField]
    public List<EntityUid> Marks = new();

    [DataField]
    public int MaxMarks = 50;

    [DataField]
    public float FillRange = 1.75f;

    [DataField]
    public bool ChainPending;

    [DataField]
    public TimeSpan CheckEvery = TimeSpan.FromSeconds(0.1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextCheck;

    [DataField]
    public float MovingVelocity = 0.1f;

    [DataField]
    public TimeSpan QueueScanEvery = TimeSpan.FromSeconds(0.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextQueueScan;

    [DataField]
    public TimeSpan RetryEvery = TimeSpan.FromSeconds(0.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRetry;

    [DataField]
    public EntityCoordinates LastTile;

    [DataField]
    public TimeSpan SweepEvery = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSweep;
}
