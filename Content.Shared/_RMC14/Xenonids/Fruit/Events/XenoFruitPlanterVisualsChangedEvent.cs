using Content.Shared._RMC14.Xenonids.Fruit.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Fruit.Events;

// Raised on the fruit planter to update their visuals (if applicable)
[ByRefEvent]
public readonly record struct XenoFruitPlanterVisualsChangedEvent(EntProtoId<XenoFruitComponent> Choice);
