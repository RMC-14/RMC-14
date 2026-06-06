using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

[RegisterComponent]
public sealed partial class XenoDespoilerCatalyzeActionComponent : Component
{
    [DataField]
    public int HypertensionCost = 1;

    [DataField]
    public TimeSpan BuffDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public EntProtoId VisualProto = "RMCEffectDespoilerCatalyze";
}
