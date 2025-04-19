using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Intel.Tech;

[Serializable, NetSerializable]
public sealed class TechPurchaseOptionBuiMsg(int tier, int index) : BoundUserInterfaceMessage
{
    public readonly int Tier = tier;
    public readonly int Index = index;
}
