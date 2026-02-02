using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Intel.Tech;

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct TechOption(
    string Name,
    string Description,
    int Cost,
    int Increase,
    bool Repurchasable,
    int CurrentCost,
    bool Purchased,
    List<object> Events,
    SpriteSpecifier.Rsi Icon,
    TimeSpan TimeLock
);
