using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.UniformAccessories;

[Serializable, NetSerializable]
public enum UniformAccessoriesUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class UniformAccessoriesBuiMsg(NetEntity toRemove) : BoundUserInterfaceMessage
{
    public readonly NetEntity ToRemove = toRemove;
}

[Serializable, NetSerializable]
public sealed class UniformAccessoriesBuiState() : BoundUserInterfaceState;
