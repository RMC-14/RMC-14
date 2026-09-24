using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.UniversalRecorder;

[RegisterComponent]
public sealed partial class UniversalRecorderTapeComponent : Component
{
    [DataField]
    public TimeSpan MaxCapacity = TimeSpan.FromMinutes(20);

    [DataField]
    public TimeSpan RespoolTime = TimeSpan.FromSeconds(5);

    [DataField]
    public ProtoId<ToolQualityPrototype> ScrewdriverQuality = "Screwing";

    [DataField]
    public SoundSpecifier FlipSound = new SoundPathSpecifier("/Audio/Items/taperecorder/tape_flip.ogg");
}
