using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Fruit;

[Serializable, NetSerializable]
public enum XenoFruitChooseUI : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class XenoFruitChooseBuiState(int count, int max) : BoundUserInterfaceState
{
    public readonly int Count = count;
    public readonly int Max = max;
}

[Serializable, NetSerializable]
public sealed class XenoFruitChooseBuiMsg(EntProtoId fruitId) : BoundUserInterfaceMessage
{
    public readonly EntProtoId FruitId = fruitId;
}
