using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.MotionDetector;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(MotionDetectorSystem))]
public sealed partial class MotionDetectorComponent : Component, IDetectorComponent
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField, AutoNetworkedField]
    public int Range;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextScanAt;

    [DataField, AutoNetworkedField]
    public bool CanToggleRange = true;

    [DataField, AutoNetworkedField]
    public bool Short;

    [DataField, AutoNetworkedField]
    public int ShortRange = 7;

    [DataField, AutoNetworkedField]
    public int LongRange = 14;

    [DataField, AutoNetworkedField]
    public TimeSpan ShortRefresh = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan LongRefresh = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan MoveTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public List<Blip> Blips { get; set; } = new();

    [DataField, AutoNetworkedField]
    public TimeSpan LastScan { get; set; }

    [DataField, AutoNetworkedField]
    public TimeSpan ScanDuration { get; set; } = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ScanSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/motion_detector.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ScanEmptySound = new SoundPathSpecifier("/Audio/_RMC14/Effects/motion_detector_none.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");

    [DataField, AutoNetworkedField]
    public bool HandToggleable = true;

    [DataField, AutoNetworkedField]
    public bool DeactivateOnDrop = true;
}

[Serializable, NetSerializable]
public enum MotionDetectorLayer
{
    Setting,
    Number,
}

[Serializable, NetSerializable]
public enum MotionDetectorSetting
{
    Short,
    Long,
}
