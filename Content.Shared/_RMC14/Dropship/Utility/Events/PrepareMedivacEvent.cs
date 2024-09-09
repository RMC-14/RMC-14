using Robust.Shared.Map;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Dropship.Utility;

/// <summary>
/// Event applied onto a MedivacStretcher to prepare to send a humanoid entity
/// to a target location. DOES NOT MOVE THE HUNANOID, for that <see cref="Content.Shared._RMC14.Dropship.Utility.Events.MedivacDoAfterEvent"/>>
/// </summary>
[Serializable, NetSerializable]
public sealed partial class PrepareMedivacEvent : EntityEventArgs
{
    /// <summary>
    /// The medivac point
    /// </summary>
    public NetEntity MedivacEntity;

    public bool ReadyForMedivac = false;

    public PrepareMedivacEvent(NetEntity medivacEntity)
    {
        MedivacEntity = medivacEntity;
    }
}
