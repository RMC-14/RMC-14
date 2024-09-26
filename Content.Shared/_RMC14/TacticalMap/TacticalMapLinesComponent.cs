using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapLinesComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<TacticalMapLine> MarineLines = new();

    [DataField, AutoNetworkedField]
    public List<TacticalMapLine> XenoLines = new();
}
