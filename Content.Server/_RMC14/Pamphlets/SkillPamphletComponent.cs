using Content.Shared.Whitelist;
using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server._RMC14.Pamphlets;

[RegisterComponent]
public sealed partial class SkillPamphletComponent : Component
{
    [DataField]
    public ComponentRegistry AddComps = new();

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> AddSkills = new();

    [DataField, AutoNetworkedField]
    public bool BypassLimit;

    [DataField, AutoNetworkedField]
    public List<PamphletWhitelist> Whitelists = new();

    [DataRecord]
    public readonly record struct PamphletWhitelist(string Popup, EntityWhitelist Restrictions);

    public bool GaveSkill;
}
