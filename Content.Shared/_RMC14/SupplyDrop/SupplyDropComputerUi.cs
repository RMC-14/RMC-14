using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.SupplyDrop;

[Serializable, NetSerializable]
public enum SupplyDropComputerUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SupplyDropComputerLongitudeBuiMsg(int longitude) : BoundUserInterfaceMessage
{
    public int Longitude = longitude;
}

[Serializable, NetSerializable]
public sealed class SupplyDropComputerLatitudeBuiMsg(int latitude) : BoundUserInterfaceMessage
{
    public int Latitude = latitude;
}

[Serializable, NetSerializable]
public sealed class SupplyDropComputerUpdateBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SupplyDropComputerLaunchBuiMsg : BoundUserInterfaceMessage;
