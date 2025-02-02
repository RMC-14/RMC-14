using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.OrbitalCannon;

[Serializable, NetSerializable]
public enum OrbitalCannonComputerUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class OrbitalCannonComputerLoadBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class OrbitalCannonComputerUnloadBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class OrbitalCannonComputerChamberBuiMsg : BoundUserInterfaceMessage;
