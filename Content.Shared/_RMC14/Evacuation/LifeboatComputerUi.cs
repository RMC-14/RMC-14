using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Evacuation;

[Serializable, NetSerializable]
public enum LifeboatComputerUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class LifeboatComputerLaunchBuiMsg : BoundUserInterfaceMessage;
