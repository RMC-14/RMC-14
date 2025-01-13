using Robust.Shared.Audio;

namespace Content.Server._RMC14.Trigger;

[RegisterComponent]
[Access(typeof(RMCTriggerSystem))]
public sealed partial class TriggerOnThrowEndComponent : Component
{
    [DataField]
    public TimeSpan Delay;

    [DataField]
    public float BeepInterval;

    [DataField]
    public float? InitialBeepDelay;

    [DataField]
    public SoundSpecifier? BeepSound;
}
