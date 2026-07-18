using Content.Shared._RMC14.Dialog;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.GroundsideOperations;

[Serializable, NetSerializable]
public enum GroundsideOperationsConsoleUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class GroundsideOperationsHighCommandMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed record GroundsideOperationsHighCommandDialogEvent(NetEntity User, string Message = "") : DialogInputEvent(Message);

[Serializable, NetSerializable]
public sealed class GroundsideOperationsGroundAnnouncementMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed record GroundsideOperationsGroundAnnouncementDialogEvent(NetEntity User, string Message = "") : DialogInputEvent(Message);

[Serializable, NetSerializable]
public sealed class GroundsideOperationsRedAlertMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class GroundsideOperationsGeneralQuartersMsg : BoundUserInterfaceMessage;
