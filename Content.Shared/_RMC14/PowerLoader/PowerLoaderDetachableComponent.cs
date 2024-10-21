using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.PowerLoader;

/// <summary>
/// For entities that can be "detached" from another thing via interaction with said entity
/// with an empty hand of the power loader
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerLoaderDetachableComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DetachDelay = TimeSpan.FromSeconds(5);
}
