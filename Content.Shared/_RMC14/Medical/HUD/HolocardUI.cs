using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.HUD;

[Serializable, NetSerializable]
public enum HolocardChangeUIKey : byte
{
    Key,
}

/// <summary>
///     Indicates to the server to change the holocard status of a entity
/// </summary>
[NetSerializable, Serializable]
public sealed class HolocardChangeEvent(NetEntity owner, HolocardStatus newHolocardStatus) : BoundUserInterfaceMessage
{
    public HolocardStatus NewHolocardStatus = newHolocardStatus;

    /// <summary>
    /// The entity changing the holocard
    /// </summary>
    public NetEntity Owner = owner;
}

/// <summary>
///     Sent by the client from any scan window (health scanner, body scanner, or medical records)
///     to ask the server to open the holocard-change UI for <see cref="Target"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class OpenHolocardFromScanEvent(NetEntity target) : EntityEventArgs
{
    public readonly NetEntity Target = target;
}
