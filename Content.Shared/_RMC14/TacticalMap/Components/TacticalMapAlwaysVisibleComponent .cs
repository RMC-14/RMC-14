using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

/// <summary>
/// Entity always visible on map for faction(s)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapAlwaysVisibleComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<TacticalMapLayerPrototype>> VisibleLayers = new();
}
