namespace Content.Client._RMC14.Light;

[RegisterComponent]
[Access(typeof(RMCTemporaryDisabledLightSystem))]
public sealed partial class RMCTemporaryDisabledLightComponent : Component
{
    [DataField]
    public TimeSpan NextCheckAt;
}
