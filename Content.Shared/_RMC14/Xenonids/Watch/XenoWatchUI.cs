using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Watch;

[Serializable, NetSerializable]
public enum XenoWatchUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public readonly record struct Xeno(NetEntity Entity, string Name, EntProtoId? Id);

[Serializable, NetSerializable]
public sealed class XenoWatchBuiState(List<Xeno> xenos, int burrowedLarva) : BoundUserInterfaceState
{
    public readonly List<Xeno> Xenos = xenos;
    public readonly int BurrowedLarva = burrowedLarva;
}

[Serializable, NetSerializable]
public sealed class XenoWatchBuiMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}
