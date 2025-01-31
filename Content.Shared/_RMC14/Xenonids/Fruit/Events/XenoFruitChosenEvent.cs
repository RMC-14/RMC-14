using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Fruit.Events;

// Raised on the action to update its icon upon fruit selection
[ByRefEvent]
public readonly record struct XenoFruitChosenEvent(EntProtoId Choice);
