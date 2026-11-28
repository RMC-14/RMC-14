using Content.Shared.Doors.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship;

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
public sealed class DropshipLockdownMsg(DoorLocation doorLocation) : BoundUserInterfaceMessage
{
    public readonly DoorLocation DoorLocation = doorLocation;
}

[Serializable, NetSerializable]
public enum DropshipNavigationUiKey
{
    Key
}

[Serializable, NetSerializable]
public readonly record struct Destination(NetEntity Id, string Name, bool Occupied, bool Primary);

[Serializable, NetSerializable]
public sealed class DropshipLaunchAlarmToggleMsg : BoundUserInterfaceMessage;
