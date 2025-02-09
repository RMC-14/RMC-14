using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Telephone;

[Serializable, NetSerializable]
public enum RMCTelephoneUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCTelephoneBuiState(List<RMCPhone> phones) : BoundUserInterfaceState
{
    public readonly List<RMCPhone> Phones = phones;
}

[Serializable, NetSerializable]
public sealed class RMCTelephoneCallBuiMsg(NetEntity id) : BoundUserInterfaceMessage
{
    public readonly NetEntity Id = id;
}

[Serializable, NetSerializable]
public readonly record struct RMCPhone(NetEntity Id, string Category, string Name);

