using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.Events;
/// <summary>
/// Called when a xeno finishes repairing a structure
/// </summary>
[Serializable, NetSerializable]
public sealed partial class XenoRepairStructureDoAfterEvent : SimpleDoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
