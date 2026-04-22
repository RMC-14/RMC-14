using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.UniversalRecorder;

[RegisterComponent]
public sealed partial class UniversalRecorderComponent : Component
{
    public const string TapeSlotId = "rmc_universal_recorder_tape";

    [DataField("tapeSlot")]
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
    public SoundSpecifier PlaySound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");

    [DataField]
    public SoundSpecifier StopSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/print.ogg");

    [DataField]
    public SoundSpecifier HissSound = new SoundPathSpecifier("/Audio/Items/hiss.ogg");
}
