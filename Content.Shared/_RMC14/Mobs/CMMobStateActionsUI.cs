using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mobs;

[Serializable, NetSerializable]
public sealed class CMGhostActionBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public enum CMMobStateActionsUI
{
    Key,
}
