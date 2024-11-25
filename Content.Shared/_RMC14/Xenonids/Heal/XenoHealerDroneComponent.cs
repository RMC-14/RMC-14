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
    /// Minimum amount needed to heal others in order to respawn after performing a sacrafice
    /// </summary>
    [DataField]
    public float HealMinimum = 7500;
}
