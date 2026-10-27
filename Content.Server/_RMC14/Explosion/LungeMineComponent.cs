using Robust.Shared.Audio;

namespace Content.Server._RMC14.Explosion;

[RegisterComponent]
public sealed partial class LungeMineComponent : Component
{
    [DataField]
    public bool GibsWielder = true;

    [DataField]
    public TimeSpan DazeDuration = TimeSpan.FromSeconds(6);

    [DataField]
    public SoundSpecifier? TriggerSound = new SoundPathSpecifier("/Audio/Items/wirecutter.ogg");
}
