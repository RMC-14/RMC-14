using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Telephone;

[Serializable, NetSerializable]
public enum RMCTelephoneUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCTelephoneBuiState(List<RMCPhone> phones, bool canDnd, bool dnd) : BoundUserInterfaceState
{
    public readonly List<RMCPhone> Phones = phones;
    public readonly bool CanDnd = canDnd;
    public readonly bool Dnd = dnd;
}

[Serializable, NetSerializable]
public sealed class RMCTelephoneCallBuiMsg(NetEntity id) : BoundUserInterfaceMessage
{
    public readonly NetEntity Id = id;
}

[Serializable, NetSerializable]
public sealed class RMCTelephoneDndBuiMsg(bool dnd) : BoundUserInterfaceMessage
{
    public readonly bool Dnd = dnd;
}

[Serializable, NetSerializable]
public readonly record struct RMCPhone(NetEntity Id, string Category, string Name);

