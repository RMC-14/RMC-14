using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.AntiAir;

[Serializable, NetSerializable]
public readonly record struct RMCShipDefenseZoneEntry(string Zone);

[Serializable, NetSerializable]
public readonly record struct RMCAlmayerAntiAirStatus(
    bool HasConsole,
    bool Disabled,
    string? ProtectedZone);

[Serializable, NetSerializable]
public sealed class RMCAlmayerAntiAirSetZoneBuiMsg(string zone) : BoundUserInterfaceMessage
{
    public readonly string Zone = zone;
}

[Serializable, NetSerializable]
public sealed class RMCAlmayerAntiAirClearZoneBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public enum RMCAlmayerAntiAirUiKey
{
    Key,
}
