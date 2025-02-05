using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Intel;

[Serializable, NetSerializable]
public enum IntelSpawnerType
{
    Close,
    Medium,
    Far,
    Science,
}
