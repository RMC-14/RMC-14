using Content.Shared.Access;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Access;

[Serializable] [NetSerializable]
public enum IdModificationConsoleUIKey
{
    Key,
}

[Serializable] [NetSerializable]
public sealed class IdModificationConsoleAccessChangeBuiMsg(ProtoId<AccessLevelPrototype> access, bool add)
    : BoundUserInterfaceMessage
{
    public readonly ProtoId<AccessLevelPrototype> Access = access;
    public readonly bool Add = add;
}

[Serializable] [NetSerializable]
public sealed class IdModificationConsoleMultipleAccessChangeBuiMsg(string type, string accessGroup)
    : BoundUserInterfaceMessage
{
    public readonly string AccessList = accessGroup;
    public readonly string Type = type;
}

[Serializable] [NetSerializable]
public sealed class IdModificationConsoleSignInBuiMsg : BoundUserInterfaceMessage;

[Serializable] [NetSerializable]
public sealed class IdModificationConsoleSignInTargetBuiMsg : BoundUserInterfaceMessage;

[Serializable] [NetSerializable]
public sealed class IdModificationConsoleIFFChangeBuiMsg(bool revoke)
    : BoundUserInterfaceMessage
{
    public readonly bool Revoke = revoke;
}

[Serializable] [NetSerializable]
public sealed class IdModificationConsoleJobChangeBuiMsg(ProtoId<AccessGroupPrototype> accessGroup)
    : BoundUserInterfaceMessage
{
    public readonly ProtoId<AccessGroupPrototype> AccessGroup = accessGroup;
}

[Serializable] [NetSerializable]
public sealed class IdModificationConsoleTerminateConfirmBuiMsg : BoundUserInterfaceMessage;

[Serializable] [NetSerializable]
public sealed class IdModificationConsoleAssignSquadMsg(NetEntity? squad) : BoundUserInterfaceMessage
{
    public readonly NetEntity? Squad = squad;
}
