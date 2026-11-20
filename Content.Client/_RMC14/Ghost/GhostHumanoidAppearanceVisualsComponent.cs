namespace Content.Client._RMC14.Ghost;

[RegisterComponent]
[Access(typeof(GhostHumanoidAppearanceVisualizerSystem))]
public sealed partial class GhostHumanoidAppearanceVisualsComponent : Component
{
    [DataField]
    public HashSet<string> RenderedLayers = new();

    [DataField]
    public HashSet<string> BoostedLayers = new();
}
