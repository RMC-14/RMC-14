using Robust.Shared.Map;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Dropship.Utility;

/// <summary>
/// Event applied onto a MedevacStretcher to prepare to send a humanoid entity
/// to a target location. DOES NOT MOVE THE HUNANOID, for that <see cref="Content.Shared._RMC14.Dropship.Utility.SharedMedevacSystem.Update(float)"/>>
/// </summary>
[Serializable, NetSerializable]
public sealed partial class PrepareMedevacEvent : EntityEventArgs
{
    /// <summary>
    /// The medevac point
    /// </summary>
    public NetEntity MedevacEntity;

    public bool ReadyForMedevac = false;

    public PrepareMedevacEvent(NetEntity medevacEntity)
    {
        MedevacEntity = medevacEntity;
    }
}
