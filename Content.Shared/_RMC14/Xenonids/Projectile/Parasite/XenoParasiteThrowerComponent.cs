using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Projectile.Parasite;

/// <summary>
/// Allows a xeno to throw parasites using the "Throw Parasite" Action
/// </summary>
[RegisterComponent]
public sealed partial class XenoParasiteThrowerComponent : Component
{
    public const string ParasiteContainerId = "parasites";

    [DataField]
    public int MaxParasites = 16;
}
