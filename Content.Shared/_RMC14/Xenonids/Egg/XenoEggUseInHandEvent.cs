using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Egg;

[Serializable, NetSerializable]
public sealed partial class XenoEggUseInHandEvent : HandledEntityEventArgs
{
    public NetEntity UsedEgg;

    public XenoEggUseInHandEvent(NetEntity usedEgg)
    {
        UsedEgg = usedEgg;
    }
}
