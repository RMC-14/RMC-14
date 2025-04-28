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
public sealed class XenoWatchBuiState(List<Xeno> xenos, int burrowedLarva, int xenoCount, FixedPoint2 tierTwoSlots ,FixedPoint2 tierThreeSlots) : BoundUserInterfaceState
{
    public readonly List<Xeno> Xenos = xenos;
    public readonly int BurrowedLarva = burrowedLarva;
    public readonly int XenoCount = xenoCount;
    public readonly FixedPoint2 TierTwoSlots = tierTwoSlots;
    public readonly FixedPoint2 TierThreeSlots = tierThreeSlots;
}

[Serializable, NetSerializable]
public sealed class XenoWatchBuiMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}
