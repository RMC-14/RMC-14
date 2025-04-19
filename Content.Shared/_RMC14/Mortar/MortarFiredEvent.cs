using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mortar;

[Serializable, NetSerializable]
public sealed class MortarFiredEvent(NetEntity mortar) : EntityEventArgs
{
    public readonly NetEntity Mortar = mortar;
}
