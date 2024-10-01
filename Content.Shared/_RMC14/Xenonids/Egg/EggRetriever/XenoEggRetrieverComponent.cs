using Robust.Shared.GameStates;
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
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoEggRetrieverComponent : Component
{
    public EntProtoId EggPrototype = "XenoEgg";

    [DataField, AutoNetworkedField]
    public int MaxEggs = 8;

    [DataField, AutoNetworkedField]
    public int CurEggs = 0;
}
