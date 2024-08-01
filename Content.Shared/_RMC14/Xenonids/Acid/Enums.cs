using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Acid;

[Serializable, NetSerializable]
public enum AcidStrength : byte
{
    Weak,
    Normal,
    Strong,
}
