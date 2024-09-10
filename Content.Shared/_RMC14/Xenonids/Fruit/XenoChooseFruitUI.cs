using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Fruit;

[Serializable, NetSerializable]
public enum XenoChooseFruitUI : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class XenoChooseFruitBuiMsg(EntProtoId fruitId) : BoundUserInterfaceMessage
{
    public readonly EntProtoId FruitId = fruitId;
}
