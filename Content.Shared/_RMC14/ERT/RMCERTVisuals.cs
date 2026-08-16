using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Appearance keys used by distress beacons to reflect their armed/sent state.
/// </summary>
[Serializable, NetSerializable]
public enum RMCERTDistressBeaconVisuals : byte
{
    /// <summary>
    /// Whether the distress beacon is lit as active/spent after sending a request.
    /// </summary>
    Active,
}
