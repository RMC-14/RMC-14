using Content.Shared._RMC14.Dropship;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Elevators;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class RMCElevatorPanelComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ElevatorId = "rmc-elevator";

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedDestination;

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedElevator;

    [DataField, AutoNetworkedField]
    public bool CallOnly = false;

    [DataField, AutoNetworkedField]
    public SoundSpecifier FailSound = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");

    [DataField, AutoNetworkedField]
    public string LinkCode = String.Empty;
}

[Serializable, NetSerializable]
public enum ElevatorPanelUiKey
{
    Key,
}

[Serializable, NetSerializable]
public readonly record struct ElevatorDestination(NetEntity Id, string Name);

[Serializable, NetSerializable]
public sealed class ElevatorDestinationsMsg(List<ElevatorDestination> destinations, NetEntity? currDestination) : BoundUserInterfaceMessage
{
    public readonly List<ElevatorDestination> Destinations = destinations;
    public readonly NetEntity? CurrDestination = currDestination;
}

[Serializable, NetSerializable]
public sealed class ElevatorTravellingMsg(FTLState state, StartEndTime time, string destination) : BoundUserInterfaceMessage
{
    public readonly FTLState State = state;
    public readonly StartEndTime Time = time;
    public readonly string Destination = destination;
}

[Serializable, NetSerializable]
public sealed class ElevatorSendMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}
