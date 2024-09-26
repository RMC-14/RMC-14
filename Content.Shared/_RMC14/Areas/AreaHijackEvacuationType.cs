using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Areas;

[Serializable, NetSerializable]
public enum AreaHijackEvacuationType
{
    None = 0,
    Add,
    Multiply,
}
