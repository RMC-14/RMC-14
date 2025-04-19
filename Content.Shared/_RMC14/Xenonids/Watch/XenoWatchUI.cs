using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Watch;

[Serializable, NetSerializable]
public enum XenoWatchUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public readonly record struct Xeno(NetEntity Entity, string Name, EntProtoId? Id, FixedPoint2 Health, FixedPoint2 Plasma, FixedPoint2 Evo);

[Serializable, NetSerializable]
public sealed class XenoWatchBuiState(List<Xeno> xenos, int burrowedLarva, int xenoCount,int tierOne , int tierTwo, FixedPoint2 tierTwoSlots ,int tierThree,FixedPoint2 tierThreeSlots) : BoundUserInterfaceState
{
    public readonly List<Xeno> Xenos = xenos;
    public readonly int BurrowedLarva = burrowedLarva;
    public readonly int XenoCount = xenoCount;
    public readonly int TierOne = tierOne;
    public readonly int TierThree = tierThree;
    public readonly int TierTwo = tierTwo;
    public readonly FixedPoint2 TierTwoSlots = tierTwoSlots;
    public readonly FixedPoint2 TierThreeSlots = tierThreeSlots;
}

[Serializable, NetSerializable]
public sealed class XenoWatchBuiMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}
