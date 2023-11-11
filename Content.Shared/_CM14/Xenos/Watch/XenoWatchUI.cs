using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenos.Watch;

[Serializable, NetSerializable]
public enum XenoWatchUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public readonly record struct Xeno(NetEntity Entity, string Name, EntProtoId? Id);

[Serializable, NetSerializable]
public sealed class XenoWatchBuiState : BoundUserInterfaceState
{
    public readonly List<Xeno> Xenos;

    public XenoWatchBuiState(List<Xeno> xenos)
    {
        Xenos = xenos;
    }
}

[Serializable, NetSerializable]
public sealed class XenoWatchBuiMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Target;

    public XenoWatchBuiMessage(NetEntity target)
    {
        Target = target;
    }
}
