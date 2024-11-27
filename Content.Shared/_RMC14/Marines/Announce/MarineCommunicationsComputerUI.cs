using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Announce;

[Serializable, NetSerializable]
public enum MarineCommunicationsComputerUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class MarineCommunicationsOpenMapMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MarineCommunicationsOverwatchMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MarineCommunicationsComputerMsg(string text) : BoundUserInterfaceMessage
{
    public readonly string Text = text;
}

[Serializable, NetSerializable]
public sealed class MarineCommunicationsDesignatePrimaryLZMsg(NetEntity lz) : BoundUserInterfaceMessage
{
    public readonly NetEntity LZ = lz;
}

[Serializable, NetSerializable]
public sealed class MarineCommunicationsComputerBuiState(string planet, string operation, List<LandingZone> landingZones) : BoundUserInterfaceState
{
    public readonly string Planet = planet;
    public readonly string Operation = operation;
    public readonly List<LandingZone> LandingZones = landingZones;
}

[Serializable, NetSerializable]
public readonly record struct LandingZone(NetEntity Id, string Name);
