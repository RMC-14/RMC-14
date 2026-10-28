namespace Content.Shared._RMC14.Chemistry.Buildup;

[RegisterComponent]
[Access(typeof(RMCHivemindInterferenceSystem))]
public sealed partial class RMCHivemindInterferenceImmuneComponent : Component
{
    [DataField]
    public float Remaining = 30f;

    [DataField]
    public float Max = 30f;

    [DataField]
    public float ReapplyExtension = 1f;
}
