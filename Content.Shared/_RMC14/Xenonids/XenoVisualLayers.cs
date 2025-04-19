using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids;

[Serializable, NetSerializable]
public enum XenoVisualLayers : byte
{
    Base,
    Hide,
    Crest,
    Fortify,
    Ovipositor,
    Burrow
}
