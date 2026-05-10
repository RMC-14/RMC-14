namespace Content.Client._RMC14.Light;

[RegisterComponent]
public sealed partial class RMCTemporaryDisabledLightComponent : Component
{
    [DataField]
    public TimeSpan NextCheckAt;
}
