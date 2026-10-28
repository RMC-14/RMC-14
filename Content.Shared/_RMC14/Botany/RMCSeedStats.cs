using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Botany;

[Serializable, NetSerializable]
public readonly record struct RMCSeedStats(
    float Endurance,
    float Lifespan,
    float Maturation,
    float Production,
    int Yield,
    HarvestType HarvestRepeat,
    bool Seedless,
    float Potency,
    bool Viable);
