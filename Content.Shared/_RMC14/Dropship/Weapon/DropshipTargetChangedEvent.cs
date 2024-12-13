using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Dropship.Weapon;

/// <summary>
/// Raised when a dropship changes targets
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DropshipTargetChangedEvent : EntityEventArgs
{
    public NetEntity? DropshipTarget = null;

    public DropshipTargetChangedEvent(NetEntity? dropshipTarget)
    {
        DropshipTarget = dropshipTarget;
    }
}
