using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Projectile.Parasite;

[Serializable, NetSerializable]
public sealed partial class XenoChangeParasiteReserveEvent : BoundUserInterfaceMessage
{
    public int NewReserve;

    public XenoChangeParasiteReserveEvent(int newReserve)
    {
        NewReserve = newReserve;
    }
}
