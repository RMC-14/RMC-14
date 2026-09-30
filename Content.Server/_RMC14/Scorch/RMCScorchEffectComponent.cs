namespace Content.Server._RMC14.Scorch;

[RegisterComponent]
public sealed partial class RMCScorchEffectComponent : Component
{
    [DataField]
    public int Count = 1;

    [DataField]
    public float ScatterRadius = 0f;

    [DataField]
    public bool RandomRotation = true;

    [DataField]
    public int Radius = 0;

    [DataField]
    public int CenterRadius = 0;

    [DataField]
    public string CenterDecalTag = "RMCScorch";

    [DataField]
    public string EdgeDecalTag = "RMCScorchSmall";
}
