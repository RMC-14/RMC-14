using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.ControlComputer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMarineControlComputerSystem))]
public sealed partial class MarineControlComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Evacuating;

    [DataField, AutoNetworkedField]
    public bool CanEvacuate;

    // TODO make new sound for this
    // [DataField, AutoNetworkedField]
    // public SoundSpecifier? EvacuationStartSound = new SoundSpecifier("/Audio/_RMC14/Announcements/ARES/evacuation_start.ogg", AudioParams.Default.WithVolume(-5));

    [DataField, AutoNetworkedField]
    public SoundSpecifier? EvacuationCancelledSound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/evacuate_cancelled.ogg", AudioParams.Default.WithVolume(-5));

    [DataField, AutoNetworkedField]
    public TimeSpan ToggleCooldown = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan LastToggle;

    [DataField, AutoNetworkedField]
    public Dictionary<string, GibbedMarineInfo> GibbedMarines = new();
}

[Serializable, NetSerializable]
public sealed class GibbedMarineInfo
{
    public string Name = string.Empty;
    public string? LastPlayerId;
}
