using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent]
public sealed partial class RMCLogComponent : Component
{
    [DataField]
    public EntProtoId SpawnedPrototype = "MaterialWoodPlank1";

    [DataField]
    public int SpawnCount = 2;
}
