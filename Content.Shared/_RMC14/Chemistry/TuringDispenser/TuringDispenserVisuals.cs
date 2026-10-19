using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.TuringDispenser;

[Serializable, NetSerializable]
public enum TuringDispenserVisuals
{
    Status,
    HasBox,
    HasBeaker,
}

[Serializable, NetSerializable]
public enum TuringDispenserVisualLayers
{
    Status,
}
