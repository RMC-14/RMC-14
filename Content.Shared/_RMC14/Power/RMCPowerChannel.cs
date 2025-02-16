using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Power;

/// <summary>
/// See the method below when modifying these values.
/// <seealso cref="SharedRMCPowerSystem.GetAreaReceivers"/>
/// </summary>
[Serializable, NetSerializable]
public enum RMCPowerChannel
{
    Equipment,
    Lighting,
    Environment,
}
