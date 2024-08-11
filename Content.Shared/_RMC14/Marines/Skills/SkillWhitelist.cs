using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Skills;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class SkillWhitelist
{
    [DataField(required: true)]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> All = new();
}
