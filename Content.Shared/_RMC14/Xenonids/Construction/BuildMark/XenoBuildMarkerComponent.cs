using Robust.Shared.Map;

namespace Content.Shared._RMC14.Xenonids.Construction.BuildMark;

[RegisterComponent]
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
    public TimeSpan QueueScanEvery = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan NextQueueScan;

    [DataField]
    public TimeSpan RetryEvery = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan NextRetry;

    [DataField]
    public EntityCoordinates LastTile;

    [DataField]
    public TimeSpan SweepEvery = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan NextSweep;
}
