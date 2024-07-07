using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent]
public sealed partial class XenoConstructionUpgradeComponent : Component
{
    [DataField]
    public EntProtoId UpgradeProto;
}
