using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenos.Construction.Events;

public sealed partial class XenoPlantWeedsActionEvent : InstantActionEvent
{
    [DataField]
    public int PlasmaCost = 75;

    [DataField]
    public EntProtoId Prototype = "XenoWeedsSourceEntity";
}
