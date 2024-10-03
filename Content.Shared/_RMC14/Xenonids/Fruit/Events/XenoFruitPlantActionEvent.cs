using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Fruit.Events;

public sealed partial class XenoFruitPlantActionEvent : InstantActionEvent
{
    [DataField]
    public bool CheckWeeds = true;

    [DataField]
    public FixedPoint2 PlasmaCost = 100;

    [DataField]
    public FixedPoint2 HealthCost = 50;
}
