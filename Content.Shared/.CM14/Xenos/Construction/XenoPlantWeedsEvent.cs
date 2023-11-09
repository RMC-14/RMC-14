using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.CM14.Xenos.Construction;

public sealed partial class XenoPlantWeedsEvent : InstantActionEvent
{
    [DataField]
    public int PlasmaCost = 75;

    [DataField]
    public EntProtoId Prototype = "XenoWeedsEntity";
}
