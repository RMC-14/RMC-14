using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction;

/// <summary>
/// Used for upgrading a resin structure to a thicker version.:
/// </summary>
[RegisterComponent]
public sealed partial class XenoConstructionUpgradeComponent : Component
{
    /// <summary>
    /// The prototypeId that is allowed to replace this entity.
    /// </summary>
    [DataField]
    public EntProtoId Proto;
}
