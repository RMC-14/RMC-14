using Content.Shared.Shuttles.Systems;
using Content.Shared.Timing;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship;

[Serializable, NetSerializable]
public sealed class DropshipNavigationDestinationsBuiState(List<(NetEntity Id, string Name, bool Occupied)> destinations) : BoundUserInterfaceState
{
    public readonly List<(NetEntity Id, string Name, bool Occupied)> Destinations = destinations;
}

[Serializable, NetSerializable]
public sealed class DropshipNavigationTravellingBuiState(FTLState state, StartEndTime time, string destination) : BoundUserInterfaceState
{
    public readonly FTLState State = state;
    public readonly StartEndTime Time = time;
    public readonly string Destination = destination;
}

[Serializable, NetSerializable]
public sealed class DropshipNavigationLaunchMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class DropshipLockdownMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public enum DropshipNavigationUiKey
{
    Key
}
