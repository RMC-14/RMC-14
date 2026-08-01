using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapLayerTrackedComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<TacticalMapLayerPrototype>> Layers = new();
}
