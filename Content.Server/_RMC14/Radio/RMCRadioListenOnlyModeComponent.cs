using Content.Server.Radio.EntitySystems;

namespace Content.Server._RMC14.Radio;

[RegisterComponent]
[Access(typeof(RadioDeviceSystem))]
public sealed partial class RMCRadioListenOnlyModeComponent : Component
{
    [DataField]
    public bool Enabled = false;
}
