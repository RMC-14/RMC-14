using Content.Shared._RMC14.ARES.Logs;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ARES.ExternalTerminals;

[Serializable] [NetSerializable]
public enum RMCARESExternalTerminalUIKey
{
    Key,
}

[Serializable] [NetSerializable]
public sealed class RMCARESExternalLogin() : BoundUserInterfaceMessage;

[Serializable] [NetSerializable]
public sealed class RMCARESExternalLogout() : BoundUserInterfaceMessage;

[Serializable] [NetSerializable]
public sealed class RMCARESExternalShowLogs(EntProtoId<RMCARESLogTypeComponent>? type, int index) : BoundUserInterfaceMessage
{
    public readonly EntProtoId<RMCARESLogTypeComponent>? Type = type;
    public readonly int Index = index;
}
