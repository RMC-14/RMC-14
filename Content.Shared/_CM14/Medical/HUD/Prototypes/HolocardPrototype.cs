using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._CM14.Medical.Prototypes;

/// <summary>
/// This is a prototype for defining ores that generate in rock
/// </summary>
[Prototype("holocardPrototype")]
public sealed partial class HolocardPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The icon shown on a entity with the medHUD.
    /// </summary>
    [DataField("holocardIcon")]
    public StatusIconPrototype? HolocardIcon = null;

    /// <summary>
    ///     The description shown on a health scan.
    /// </summary>
    [DataField("Description")]
    public LocId Description = "hc-none-description";
}
