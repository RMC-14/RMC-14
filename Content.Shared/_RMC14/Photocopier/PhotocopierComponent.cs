using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._RMC14.Photocopier;
/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent ]
public sealed partial class PhotocopierComponent : Component
{

    /// <summary>
    /// Contains the item to be sent, assumes it's paper...
    /// </summary>
    [DataField(required: true)]
    public string PaperSlotId = "PhotocopierSlot";

    /// <summary>
    /// Sound to play when fax printing new message
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    /// <summary>
    /// Remaining time of printing animation
    /// </summary>
    [DataField]
    public TimeSpan NextPrintAt = TimeSpan.Zero;

    /// <summary>
    /// How long the printing animation will play
    /// </summary>
    [ViewVariables]
    public TimeSpan PrintingTime = new(0,0,0,2,300);

    [ViewVariables]
    public int PrintingCount = 0;

    [ViewVariables]
    public CopyPrintout? Printout;

    /// <summary>
    ///     The prototype ID to use for faxed or copied entities if we can't get one from
    ///     the paper entity for whatever reason.
    /// </summary>
    [DataField]
    public EntProtoId PrintPaperId = "CMPaper";
}

[DataDefinition]
public sealed partial class CopyPrintout
{
    [DataField(required: true)]
    public string Name { get; private set; } = default!;

    [DataField]
    public string? Label { get; private set; }

    [DataField(required: true)]
    public string Content { get; private set; } = default!;

    [DataField("stampState")]
    public string? StampState { get; private set; }

    [DataField("stampedBy")]
    public List<StampDisplayInfo> StampedBy { get; private set; } = new();

    [DataField]
    public bool Locked { get; private set; }

    private CopyPrintout()
    {
    }

    public CopyPrintout(string content, string name, string? label = null, string? stampState = null, List<StampDisplayInfo>? stampedBy = null, bool locked = false)
    {
        Content = content;
        Name = name;
        Label = label;
        StampState = stampState;
        StampedBy = stampedBy ?? new List<StampDisplayInfo>();
        Locked = locked;
    }
}
