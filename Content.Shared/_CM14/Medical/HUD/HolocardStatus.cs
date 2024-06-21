using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Medical.HUD;

[Serializable, NetSerializable]
public enum HolocardStatus : byte
{
    None,
    Urgent,
    Emergency,
    Xeno,
    Permadead,
}
