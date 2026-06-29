using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared._RMC14.Elevators;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCElevatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ElevatorId = "rmc-elevator";

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentDestination;

    [DataField, AutoNetworkedField]
    public float StartupTime = 5.5f;

    [DataField, AutoNetworkedField]
    public float TravelTime = 20f;

    [DataField, AutoNetworkedField]
    public float ArrivalTime = 5f;

    [DataField, AutoNetworkedField]
    public float CooldownTime = 10f;

    [DataField, AutoNetworkedField]
    public Vector2? DestinationOffset;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ArrivalSound;

    [DataField, AutoNetworkedField]
    public EntityUid? ArrivalSoundEnt;
}
