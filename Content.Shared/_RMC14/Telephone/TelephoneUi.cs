using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Telephone;

[Serializable, NetSerializable]
public enum TelephoneUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class TelephoneBuiState(List<Phone> phones) : BoundUserInterfaceState
{
    public readonly List<Phone> Phones = phones;
}

[Serializable, NetSerializable]
public sealed class TelephoneCallBuiMsg(NetEntity id) : BoundUserInterfaceMessage
{
    public readonly NetEntity Id = id;
}

[Serializable, NetSerializable]
public readonly record struct Phone(NetEntity Id, string Category, string Name);

