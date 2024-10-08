using Robust.Shared.Audio;

namespace Content.Server._RMC14.Trigger;

[RegisterComponent]
[Access(typeof(RMCTriggerSystem))]
public sealed partial class OnShootTriggerAmmoTimerComponent : Component
{
    [DataField]
    public float Delay;

    [DataField]
    public float BeepInterval;

    [DataField]
    public float? InitialBeepDelay;

    [DataField]
    public SoundSpecifier? BeepSound;
}
