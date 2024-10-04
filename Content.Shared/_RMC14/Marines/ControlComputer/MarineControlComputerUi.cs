using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.ControlComputer;

[Serializable, NetSerializable]
public enum MarineControlComputerUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class MarineControlComputerToggleEvacuationMsg : BoundUserInterfaceMessage;
