using Content.Shared.DoAfter;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mortar;

[Serializable, NetSerializable]
public sealed partial class LinkMortarLaserDesignatorDoAfterEvent : DoAfterEvent
{
    public NetEntity LaserDesignator;

    public LinkMortarLaserDesignatorDoAfterEvent(NetEntity laserDesignator)
    {
        LaserDesignator = laserDesignator;
    }

    public override DoAfterEvent Clone() => this;
}
