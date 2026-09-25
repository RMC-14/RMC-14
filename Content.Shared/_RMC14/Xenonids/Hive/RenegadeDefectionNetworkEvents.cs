using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Hive;

[Serializable, NetSerializable]
public sealed class RenegadeDefectionOfferEvent : EntityEventArgs
{
    public NetEntity Xeno;
    public string Faction = string.Empty;
    public double ExpiresAt;
}

[Serializable, NetSerializable]
public sealed class RenegadeDefectionChoiceEvent : EntityEventArgs
{
    public NetEntity Xeno;
    public bool Defect;
}

[Serializable, NetSerializable]
public sealed class RenegadeDefectionOfferExpiredEvent : EntityEventArgs
{
    public NetEntity Xeno;
}
