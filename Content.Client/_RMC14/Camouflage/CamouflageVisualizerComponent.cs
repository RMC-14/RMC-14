using Robust.Shared.GameStates;

namespace Content.Client._RMC14.Camouflage;

[RegisterComponent]
public sealed partial class CamouflageVisualizerComponent : Component
{
    // DIY GenericVisualizer
    [DataField("visuals", required: true)]
    public Dictionary<Enum, Dictionary<string, Dictionary<string, PrototypeLayerData>>> Visuals = default!;
}
