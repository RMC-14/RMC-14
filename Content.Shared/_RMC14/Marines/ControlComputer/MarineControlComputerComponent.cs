using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.ControlComputer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMarineControlComputerSystem))]
public sealed partial class MarineControlComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Evacuating;

    [DataField, AutoNetworkedField]
    public bool CanEvacuate;

    // [DataField, AutoNetworkedField]
    // public SoundSpecifier? EvacuationCancelledSound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/evacuation_cancelled.ogg", AudioParams.Default.WithVolume(-5));

    [DataField, AutoNetworkedField]
    public TimeSpan ToggleCooldown = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan LastToggle;
}
