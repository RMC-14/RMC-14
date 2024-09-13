using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Fruit.Events;

[Serializable, NetSerializable]
public sealed partial class XenoPlantFruitDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetCoordinates Coordinates;

    public XenoPlantFruitDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
