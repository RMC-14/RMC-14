using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapCanvasComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<TacticalMapStroke> Strokes = new();

    [DataField, AutoNetworkedField]
    public Dictionary<Vector2i, TacticalMapLabelData> Labels = new();

    [DataField, AutoNetworkedField]
    public int Version;

    [DataField, AutoNetworkedField]
    public TimeSpan LastSubmitAt;

    [DataField, AutoNetworkedField]
    public TimeSpan NextSubmitAt;
}
