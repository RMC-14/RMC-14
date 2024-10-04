using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Evacuation;

[Serializable, NetSerializable]
public enum EvacuationComputerUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class EvacuationComputerLaunchBuiMsg : BoundUserInterfaceMessage;
