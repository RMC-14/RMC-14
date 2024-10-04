using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Power;

[Serializable, NetSerializable]
public enum RMCPowerMode
{
    Off = 0,
    Idle,
    Active,
}
