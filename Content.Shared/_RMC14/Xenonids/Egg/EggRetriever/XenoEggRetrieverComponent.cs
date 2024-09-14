using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Egg;

/// <summary>
/// Allows a xeno to stash eggs into an internal inventory and bring them back out
/// </summary>
[RegisterComponent]
public sealed partial class XenoEggRetrieverComponent : Component
{
    public const string EggContainerId = "eggs";

    [DataField]
    public EntProtoId EggPrototype = "XenoEgg";

    [DataField]
    public int MaxEggs = 7;
}
