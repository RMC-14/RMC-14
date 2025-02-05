using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship;

[Serializable, NetSerializable]
public sealed class DropshipHijackerBuiState(List<(NetEntity Id, string Name)> destinations) : BoundUserInterfaceState
{
    public List<(NetEntity Id, string Name)> Destinations = destinations;
}

[Serializable, NetSerializable]
public sealed class DropshipHijackerDestinationChosenBuiMsg(NetEntity destination) : BoundUserInterfaceMessage
{
    public NetEntity Destination = destination;
}

[Serializable, NetSerializable]
public enum DropshipHijackerUiKey
{
    Key,
}
