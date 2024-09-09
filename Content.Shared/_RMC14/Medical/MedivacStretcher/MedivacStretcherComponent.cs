using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Medical.MedivacStretcher;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MedivacStretcherComponent : Component
{
    public const string AnimationState = "winched_stretcher";
    public const string BuckledSlotId = "rmc_medivac_stretcher_buckled_entity";

    /// <summary>
    /// Cas Target ID
    /// </summary>
    [AutoNetworkedField]
    public int Id;
}
