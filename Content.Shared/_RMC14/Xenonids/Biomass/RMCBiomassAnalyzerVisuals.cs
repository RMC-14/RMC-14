using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Biomass;

[Serializable, NetSerializable]
public enum RMCBiomassAnalyzerVisuals
{
    OrganLoaded,
}

[Serializable, NetSerializable]
public enum RMCBiomassAnalyzerVisualState
{
    Idle,
    Processing,
    Printing,
}

public enum RMCBiomassAnalyzerVisualLayers
{
    Organ,
    Animation,
}
