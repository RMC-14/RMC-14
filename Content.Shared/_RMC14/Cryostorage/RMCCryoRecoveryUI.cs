using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Cryostorage;

[Serializable, NetSerializable]
public enum RMCCryoRecoveryUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCCryoRecoveryBuiState(List<RMCCryoRecoveryPlayerData> players) : BoundUserInterfaceState
{
    public readonly List<RMCCryoRecoveryPlayerData> Players = players;
}

[Serializable, NetSerializable]
public readonly record struct RMCCryoRecoveryPlayerData(
    NetEntity Player,
    string Name,
    string Job,
    List<RMCCryoRecoveryItemData> Items);

[Serializable, NetSerializable]
public readonly record struct RMCCryoRecoveryItemData(
    NetEntity Item,
    string Name,
    string Location);

[Serializable, NetSerializable]
public sealed class RMCCryoRecoveryRecoverItemBuiMsg(NetEntity player, NetEntity item) : BoundUserInterfaceMessage
{
    public readonly NetEntity Player = player;
    public readonly NetEntity Item = item;
}

[Serializable, NetSerializable]
public sealed class RMCCryoRecoveryRecoverAllBuiMsg(NetEntity player) : BoundUserInterfaceMessage
{
    public readonly NetEntity Player = player;
}
