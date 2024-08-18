using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Camera;

[Serializable, NetSerializable]
public enum RMCCameraUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCCameraWatchBuiMsg(NetEntity camera) : BoundUserInterfaceMessage
{
    public readonly NetEntity Camera = camera;
}

[Serializable, NetSerializable]
public sealed class RMCCameraPreviousBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCCameraNextBuiMsg : BoundUserInterfaceMessage;
