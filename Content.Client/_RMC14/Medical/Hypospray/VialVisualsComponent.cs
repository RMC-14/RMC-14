namespace Content.Client._RMC14.Medical.Hypospray;

[RegisterComponent]
[Access(typeof(VialVisualizerSystem))]
public sealed partial class VialVisualsComponent : Component
{
    [DataField]
    public string EmptyState = "empty";

    [DataField]
    public string VialState = "hypospray";
}
