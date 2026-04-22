using Content.Shared._RMC14.UniversalRecorder;

namespace Content.Server._RMC14.UniversalRecorder;

[RegisterComponent]
[Access(typeof(UniversalRecorderSystem))]
public sealed partial class UniversalRecorderRuntimeComponent : Component
{
    public UniversalRecorderState State = UniversalRecorderState.Stopped;
    public bool WarningSent;
    public TimeSpan RecordingBaseOffset = TimeSpan.Zero;
    public TimeSpan RecordingStartedAt = TimeSpan.Zero;
    public TimeSpan NextPlaybackAt = TimeSpan.Zero;
    public TimeSpan NextPrintAt = TimeSpan.Zero;
    public int PlaybackIndex;
    public int? PendingSilenceSeconds;
    public EntityUid? PlaybackStream;
}
