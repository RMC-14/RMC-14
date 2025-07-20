using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Doors;

[Serializable, NetSerializable]
public sealed class RMCPodDoorButtonPressedEvent(NetEntity button, string animationState) : EntityEventArgs
{
    public readonly NetEntity Button = button;
    public readonly string AnimationState = animationState;
}
