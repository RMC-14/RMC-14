using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenonids.Construction.Events;

[Serializable, NetSerializable]
public sealed partial class XenoConstructionAddPlasmaDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
