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
public readonly record struct Xeno(NetEntity Entity, string Name, EntProtoId? Id, FixedPoint2 Health, FixedPoint2 Plasma, FixedPoint2 Evo, bool Leader = false, EntProtoId? StrainOf = null);

[Serializable, NetSerializable]
public sealed class XenoWatchBuiState(List<Xeno> xenos, int burrowedLarva, int burrowedLarvaSlotFactor, int xenoCount, int tierTwoAmount, int tierThreeAmount, bool isQueen = false ) : BoundUserInterfaceState
{
    public readonly List<Xeno> Xenos = xenos;
    public readonly int BurrowedLarva = burrowedLarva;
    public readonly int BurrowedLarvaSlotFactor = burrowedLarvaSlotFactor;
    public readonly int XenoCount = xenoCount;
    public readonly int TierTwoAmount = tierTwoAmount;
    public readonly int TierThreeAmount = tierThreeAmount;
    public readonly bool IsQueen = isQueen;
}

[Serializable, NetSerializable]
public sealed class XenoWatchBuiMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class XenoWatchBuiHealingMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class XenoWatchBuiTransferPlasmaMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}
