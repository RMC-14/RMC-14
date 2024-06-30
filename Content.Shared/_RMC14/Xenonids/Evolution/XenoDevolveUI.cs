using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[Serializable, NetSerializable]
public enum XenoDevolveUIKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class XenoDevolveBuiMsg(EntProtoId choice) : BoundUserInterfaceMessage
{
    public readonly EntProtoId Choice = choice;
}
