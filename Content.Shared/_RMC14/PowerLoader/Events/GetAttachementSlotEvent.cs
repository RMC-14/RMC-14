using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.PowerLoader;

/// <summary>
/// Get the slot that an entity will be placed within or removed using a powerloader
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GetAttachementSlotEvent : EntityEventArgs
{
    /// <summary>
    /// Entity attempting to peform the attachment
    /// </summary>
    public NetEntity User;

    /// <summary>
    /// Entity that will be removed or placed
    /// </summary>
    public NetEntity? Used = null;

    public bool BeingAttached = true;

    public string SlotId = "";

    //If the slot returned is able to have something placed/be removed depending on the mode
    public bool CanUse = true;

    public GetAttachementSlotEvent(NetEntity user, NetEntity? used)
    {
        User = user;
        Used = used;
    }
}
