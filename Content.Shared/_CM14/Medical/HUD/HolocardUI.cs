using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Medical.HUD;

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
///     Indicates to the server to change open a Holocard Change Bound UI for a particular user on a particular target
/// </summary>
[NetSerializable, Serializable]
public sealed class OpenChangeHolocardUIEvent : BoundUserInterfaceMessage
{
    public NetEntity Owner;
    public NetEntity Target;

    public OpenChangeHolocardUIEvent(NetEntity owner, NetEntity target)
    {
        Owner = owner;
        Target = target;
    }
}
