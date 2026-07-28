using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Construction;

[Serializable, NetSerializable]
public enum RMCConstructionFailureReason : byte
{
    Unknown,
    MissingMaterials,
    InvalidConstructionItem,
    NotOnSameTile,
    InvalidLocation,
    SkillMissing,
    ConstructionDisabled,
    Cancelled
}
