using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.PowerLoader;

/// <summary>
/// For entities that can be "attached" to another thing via interaction with said entity
/// in-hand of the power loader
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerLoaderAttachableComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan AttachDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Target entities with any tag within this field may be attached to with this entity
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>> AttachableTypes = new();
}
