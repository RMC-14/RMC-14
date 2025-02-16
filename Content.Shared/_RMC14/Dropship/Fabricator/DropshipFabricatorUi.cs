using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship.Fabricator;

[Serializable, NetSerializable]
public enum DropshipFabricatorUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class DropshipFabricatorPrintMsg(EntProtoId id) : BoundUserInterfaceMessage
{
    public readonly EntProtoId Id = id;
}
