using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Rangefinder.Spotting;

/// <summary>
///     Raised on the client to indicate it'd like to spot a target.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestSpotEvent : EntityEventArgs
{
    public NetEntity SpottingTool;
    public NetEntity User;
    public NetEntity Target;
}
