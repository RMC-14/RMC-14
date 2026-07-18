using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.GroundsideOperations;

[Serializable, NetSerializable]
public enum GroundsideOperationsConsoleUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class GroundsideOperationsOpenOverwatchMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class GroundsideOperationsHighCommandMsg(string message) : BoundUserInterfaceMessage
{
    public readonly string Message = message;
}

[Serializable, NetSerializable]
public sealed class GroundsideOperationsRedAlertMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class GroundsideOperationsGeneralQuartersMsg : BoundUserInterfaceMessage;
