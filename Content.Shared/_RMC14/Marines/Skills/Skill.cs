using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Skills;

[Serializable, NetSerializable]
public readonly record struct Skill(EntProtoId<SkillDefinitionComponent> Type, int Level);
