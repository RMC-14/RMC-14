using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.ControlComputer;

[Serializable, NetSerializable]
public enum MarineControlComputerUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class MarineControlComputerAlertLevelMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MarineControlComputerShipAnnouncementMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed record MarineControlComputerShipAnnouncementDialogEvent(NetEntity User, string Message = "") : DialogInputEvent(Message);

[Serializable, NetSerializable]
public sealed class MarineControlComputerMedalMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MarineControlComputerToggleEvacuationMsg : BoundUserInterfaceMessage;
