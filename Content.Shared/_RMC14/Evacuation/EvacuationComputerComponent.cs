using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Evacuation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedEvacuationSystem))]
public sealed partial class EvacuationComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EvacuationComputerMode Mode = EvacuationComputerMode.Disabled;

    [DataField, AutoNetworkedField]
    public int? MaxMobs;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? WarmupSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/escape_pod_warmup.ogg", AudioParams.Default.WithVolume(-4));

    [DataField, AutoNetworkedField]
    public SoundSpecifier? LaunchSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/escape_pod_launch.ogg", AudioParams.Default.WithVolume(-4));

    [DataField, AutoNetworkedField]
    public TimeSpan DetonateDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan EjectDelay = TimeSpan.FromSeconds(5.5);

    [DataField, AutoNetworkedField]
    public float EarlyCrashChance = 0.75f;
}

[Serializable, NetSerializable]
public enum EvacuationComputerMode
{
    Disabled = 0,
    Ready,
    Travelling,
    Crashed,
}
