using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Acid;

[RegisterComponent]
[Access(typeof(XenoAcidHoleSystem))]
public sealed partial class XenoAcidHoleWallComponent : Component
{
    [DataField]
    public EntProtoId HolePrototype = "RMCAcidHole";

    [DataField]
    public float DamageNearCapRatio = 0.9f;

    public EntityUid? Hole;
    public Direction? PendingDirection;
}
