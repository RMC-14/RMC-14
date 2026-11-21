namespace Content.Client._RMC14.Ghost;

[RegisterComponent]
[Access(typeof(GhostOverlayVisualizerSystem))]
public sealed partial class GhostOverlayVisualsComponent : Component
{
    [DataField]
    public HashSet<string> Layers = new();
}
