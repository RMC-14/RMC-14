using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Doors;

[Serializable, NetSerializable]
public sealed class RMCPodDoorButtonPressedEvent(NetEntity button) : EntityEventArgs
{
    public readonly NetEntity Button = button;
}
