using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.UniformAccessories;

[Serializable, NetSerializable]
public enum UniformAccessoriesUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class UniformAccessoriesBuiMsg : BoundUserInterfaceMessage
{
    [DataField]
    public readonly NetEntity ToRemove;

    public UniformAccessoriesBuiMsg(NetEntity toRemove)
    {
        ToRemove = toRemove;
    }
}

[Serializable, NetSerializable]
public sealed class UniformAccessoriesBuiState() : BoundUserInterfaceState;
