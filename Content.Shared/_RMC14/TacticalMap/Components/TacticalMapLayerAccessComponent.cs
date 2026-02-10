using System.Collections.Generic;
using Content.Shared._RMC14.Marines.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TacticalMapLayerAccessSystem), typeof(IdModificationConsoleSystem))]
public sealed partial class TacticalMapLayerAccessComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<TacticalMapLayerPrototype>> Layers = new();
}
