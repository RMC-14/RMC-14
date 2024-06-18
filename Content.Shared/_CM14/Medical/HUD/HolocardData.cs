using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Medical;

/// <summary>
/// This is a class that holds holocard data
public sealed partial class HolocardData
{
    /// <summary>
    ///     The prototype id of the icon shown on a entity with the medHUD.
    /// </summary>
    public ProtoId<StatusIconPrototype>? HolocardIconPrototype = null;

    /// <summary>
    ///     The description shown on a health scan.
    /// </summary>
    public LocId Description = "hc-none-description";
}
