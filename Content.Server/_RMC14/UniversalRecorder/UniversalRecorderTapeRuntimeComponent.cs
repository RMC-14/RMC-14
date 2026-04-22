using Content.Shared._RMC14.UniversalRecorder;

namespace Content.Server._RMC14.UniversalRecorder;

[RegisterComponent]
[Access(typeof(UniversalRecorderSystem))]
public sealed partial class UniversalRecorderTapeRuntimeComponent : Component
{
    public UniversalRecorderTapeSide Side = UniversalRecorderTapeSide.Front;
    public TimeSpan UsedCapacity = TimeSpan.Zero;
    public TimeSpan OtherSideUsedCapacity = TimeSpan.Zero;
    public bool Unspooled;
    public string? FrontName;
    public string? BackName;
    public List<RecorderEntry> Entries = new();
    public List<RecorderEntry> OtherSideEntries = new();
}
