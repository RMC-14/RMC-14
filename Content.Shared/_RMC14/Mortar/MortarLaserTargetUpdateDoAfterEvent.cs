using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mortar;

[Serializable, NetSerializable]
public sealed partial class MortarLaserTargetUpdateDoAfterEvent : DoAfterEvent
{
    public NetCoordinates TargetCoordinates;

    public MortarLaserTargetUpdateDoAfterEvent(NetCoordinates targetCoordinates)
    {
        TargetCoordinates = targetCoordinates;
    }

    public override DoAfterEvent Clone()
    {
        return new MortarLaserTargetUpdateDoAfterEvent(TargetCoordinates);
    }
}
