using System.Collections.Generic;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapActiveLayerLinesComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<TacticalMapLine> Lines = new();
}
