using Content.Shared.FixedPoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Heal;

/// <summary>
/// Mainly for the drone healer strain.
/// </summary>
[RegisterComponent]
public sealed partial class XenoSacraficialHealerComponent : Component
{
    /// <summary>
    /// What proportion of current health will be sent to target
    /// </summary>
    [DataField]
    public FixedPoint2 TransferProportion = 0.75;
}
