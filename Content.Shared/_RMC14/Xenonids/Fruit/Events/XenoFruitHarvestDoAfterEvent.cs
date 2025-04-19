using Content.Shared._RMC14.Xenonids.Fruit.Components;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Fruit.Events;

[Serializable, NetSerializable]
public sealed partial class XenoFruitHarvestDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
