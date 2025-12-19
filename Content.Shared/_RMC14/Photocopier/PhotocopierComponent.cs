using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._RMC14.Photocopier;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FaxMachineComponent : Component
{

    /// <summary>
    /// Contains the item to be sent, assumes it's paper...
    /// </summary>
    [DataField(required: true)]
    public ItemSlot PaperSlot = new();

    /// <summary>
    /// Sound to play when fax printing new message
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    /// <summary>
    /// Sound to play when fax successfully send message
    /// </summary>
    [DataField]
    public SoundSpecifier SendSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg");

    /// <summary>
    /// Print queue of the incoming message
    /// </summary>
    [ViewVariables]
    [DataField]
    public Queue<FaxPrintout> PrintingQueue { get; private set; } = new();

    /// <summary>
    /// Message sending timeout
    /// </summary>
    [ViewVariables]
    [DataField]
    public float SendTimeoutRemaining;

    /// <summary>
    /// Message sending timeout
    /// </summary>
    [ViewVariables]
    [DataField]
    public float SendTimeout = 5f;

    /// <summary>
    /// Remaining time of printing animation
    /// </summary>
    [DataField]
    public float PrintingTimeRemaining;

    /// <summary>
    /// How long the printing animation will play
    /// </summary>
    [ViewVariables]
    public float PrintingTime = 2.3f;

    /// <summary>
    ///     The prototype ID to use for faxed or copied entities if we can't get one from
    ///     the paper entity for whatever reason.
    /// </summary>
    [DataField]
    public EntProtoId PrintPaperId = "Paper";

    /// <summary>
    ///     The prototype ID to use for faxed or copied entities if we can't get one from
    ///     the paper entity for whatever reason of the Office type.
    /// </summary>
    [DataField]
    public EntProtoId PrintOfficePaperId = "PaperOffice";
}

[DataDefinition]
public sealed partial class FaxPrintout
{
    [DataField(required: true)]
    public string Name { get; private set; } = default!;

    [DataField]
    public string? Label { get; private set; }

    [DataField(required: true)]
    public string Content { get; private set; } = default!;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
    public string PrototypeId { get; private set; } = default!;

    [DataField("stampState")]
    public string? StampState { get; private set; }

    [DataField("stampedBy")]
    public List<StampDisplayInfo> StampedBy { get; private set; } = new();

    [DataField]
    public bool Locked { get; private set; }

    private FaxPrintout()
    {
    }

    public FaxPrintout(string content, string name, string? label = null, string? prototypeId = null, string? stampState = null, List<StampDisplayInfo>? stampedBy = null, bool locked = false)
    {
        Content = content;
        Name = name;
        Label = label;
        PrototypeId = prototypeId ?? "";
        StampState = stampState;
        StampedBy = stampedBy ?? new List<StampDisplayInfo>();
        Locked = locked;
    }
}
