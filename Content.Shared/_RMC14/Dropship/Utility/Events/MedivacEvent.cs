using Robust.Shared.Map;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Dropship.Utility;

/// <summary>
/// Event applied onto a MedivacStretcher to send a humanoid entity
/// to a target location
/// </summary>
[Serializable, NetSerializable]
public sealed partial class MedivacEvent : EntityEventArgs
{
    /// <summary>
    /// Where to medivac a patient
    /// </summary>
    public NetCoordinates MedivacCordinates;

    public bool SucessfulMedivac = false;

    public MedivacEvent(NetCoordinates medivacCordinates)
    {
        MedivacCordinates = medivacCordinates;
    }
}
