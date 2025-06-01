using Content.Shared.Alert;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Tracker;

[Prototype]
public sealed partial class TrackerModePrototype : IPrototype
{
    /// <summary>
    ///     The tracker type.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The job that should be tracked.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? Job { get; private set; }

    /// <summary>
    ///     The component that should be tracked.
    /// </summary>
    [DataField(customTypeSerializer: typeof(ComponentNameSerializer))]
    public string? Component;

    /// <summary>
    /// The alert type that should be shown.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert { get; private set; } = "SquadTracker";
}
