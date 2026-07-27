using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Cryostorage;

[Serializable, NetSerializable]
public enum RMCCryoRecoveryUiKey : byte
{
    Key,
}

/// <summary>
/// Snapshot of all recoverable players and items visible to a specific console after server-side filtering.
/// </summary>
[Serializable, NetSerializable]
public sealed class RMCCryoRecoveryBuiState(List<RMCCryoRecoveryPlayerData> players) : BoundUserInterfaceState
{
    public readonly List<RMCCryoRecoveryPlayerData> Players = players;
}

/// <summary>
/// One stored player visible in the console, with only items that passed recovery validation.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct RMCCryoRecoveryPlayerData(
    NetEntity Player,
    string Name,
    string Job,
    List<RMCCryoRecoveryItemData> Items);

/// <summary>
/// One existing item entity that can be moved out of cryostorage. Location is kept for search/debugging,
/// but the client UI may choose not to display it.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct RMCCryoRecoveryItemData(
    NetEntity Item,
    string Name,
    string Location);

/// <summary>
/// Requests moving a single existing item from the stored body. The server re-validates ownership and access.
/// </summary>
[Serializable, NetSerializable]
public sealed class RMCCryoRecoveryRecoverItemBuiMsg(NetEntity player, NetEntity item) : BoundUserInterfaceMessage
{
    public readonly NetEntity Player = player;
    public readonly NetEntity Item = item;
}

/// <summary>
/// Requests moving every currently recoverable item for one stored player. Each item is still validated individually.
/// </summary>
[Serializable, NetSerializable]
public sealed class RMCCryoRecoveryRecoverAllBuiMsg(NetEntity player) : BoundUserInterfaceMessage
{
    public readonly NetEntity Player = player;
}
