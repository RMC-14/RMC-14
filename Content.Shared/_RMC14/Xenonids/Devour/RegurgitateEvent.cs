using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Devour;

[Serializable, NetSerializable]
public sealed partial class RegurgitateEvent : EntityEventArgs
{
    public NetEntity NetRegurgitater;

    public NetEntity NetRegurgitated;

    public RegurgitateEvent(NetEntity netRegurgitater, NetEntity netRegurgitated)
    {
        NetRegurgitater = netRegurgitater;
        NetRegurgitated = netRegurgitated;
    }
}
