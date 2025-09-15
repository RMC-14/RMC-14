using Content.Shared.Shuttles.Systems;
using Content.Shared.Timing;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship;

[Serializable, NetSerializable]
public sealed class DropshipNavigationDestinationsBuiState(NetEntity? flyBy, List<Destination> destinations) : BoundUserInterfaceState
{
    public readonly NetEntity? FlyBy = flyBy;
    public readonly List<Destination> Destinations = destinations;
}

[Serializable, NetSerializable]
public sealed class DropshipNavigationTravellingBuiState(FTLState state, StartEndTime time, string destination, string departureLocation) : BoundUserInterfaceState
{
    public readonly FTLState State = state;
    public readonly StartEndTime Time = time;
    public readonly string Destination = destination;
    public readonly string DepartureLocation = departureLocation;
}

[Serializable, NetSerializable]
public sealed class DropshipNavigationLaunchMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class DropshipNavigationCancelMsg : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class DropshipLockdownMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public enum DropshipNavigationUiKey
{
    Key
}

[Serializable, NetSerializable]
public readonly record struct Destination(NetEntity Id, string Name, bool Occupied, bool Primary);
