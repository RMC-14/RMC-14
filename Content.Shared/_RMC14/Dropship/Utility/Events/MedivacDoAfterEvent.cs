using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Dropship.Utility.Events;

/// <summary>
/// Moves a target humanoid to the Medevac Utility Entity after the animation SHOULD be completed
/// </summary>
[Serializable, NetSerializable]
public sealed partial class MedivacDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity UtilityAttachmentPoint;

    public MedivacDoAfterEvent(NetEntity utilityAttachmentPoint)
    {
        UtilityAttachmentPoint = utilityAttachmentPoint;
    }
    public override DoAfterEvent Clone()
    {
        var doAfter = (MedivacDoAfterEvent) base.Clone();
        doAfter.UtilityAttachmentPoint = this.UtilityAttachmentPoint;
        return doAfter;
    }
}
