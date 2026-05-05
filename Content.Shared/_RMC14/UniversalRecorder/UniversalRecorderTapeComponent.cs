using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.UniversalRecorder;

[RegisterComponent, NetworkedComponent]
public sealed partial class UniversalRecorderTapeComponent : Component
{
    [DataField]
    public TimeSpan MaxCapacity = TimeSpan.FromMinutes(20);

    [DataField]
    public TimeSpan RespoolTime = TimeSpan.FromSeconds(5);

    [DataField]
    public string ScrewdriverQuality = "Screwing";

    [DataField]
    public SoundSpecifier FlipSound = new SoundPathSpecifier("/Audio/Items/taperecorder/tape_flip.ogg");
}
