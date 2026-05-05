using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.UniversalRecorder;

[RegisterComponent, NetworkedComponent]
public sealed partial class UniversalRecorderComponent : Component
{
    public const string TapeSlotId = "rmc_universal_recorder_tape";

    [DataField]
    public ItemSlot TapeSlot = new();

    [DataField]
    public EntProtoId<PaperComponent> PrintoutPrototype = "CMPaper";

    [DataField]
    public float ListenRange = 10f;

    [DataField]
    public TimeSpan WarningThreshold = TimeSpan.FromMinutes(1);

    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(30);

    [DataField]
    public TimeSpan PlaybackSilenceThreshold = TimeSpan.FromSeconds(14);

    [DataField]
    public SoundSpecifier PlaySound = new SoundPathSpecifier("/Audio/Items/taperecorder/taperecorder_play.ogg");

    [DataField]
    public SoundSpecifier StopSound = new SoundPathSpecifier("/Audio/Items/taperecorder/taperecorder_stop.ogg");

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Items/taperecorder/taperecorder_print.ogg");

    [DataField]
    public SoundSpecifier HissStartSound = new SoundPathSpecifier("/Audio/Items/taperecorder/taperecorder_hiss_start.ogg");

    [DataField]
    public SoundSpecifier HissLoopSound = new SoundPathSpecifier("/Audio/Items/taperecorder/taperecorder_hiss_mid.ogg");

    [DataField]
    public TimeSpan HissStartDelay = TimeSpan.FromMilliseconds(250);
}
