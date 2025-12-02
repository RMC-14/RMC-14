using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Skills.Pamphlets;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SkillPamphletComponent : Component
{
    [DataField]
    public ComponentRegistry AddComps = new();

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> AddSkills = new();

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> SkillCap = new();

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? GiveIcon;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? GiveMapBlip;

    [DataField, AutoNetworkedField]
    public LocId? GiveJobTitle;

    [DataField, AutoNetworkedField]
    public LocId? GivePrefix;

    [DataField, AutoNetworkedField]
    public bool IsAppendPrefix = false;

    [DataField, AutoNetworkedField]
    public bool BypassLimit;

    [DataField, AutoNetworkedField]
    public List<PamphletWhitelist> Whitelists = new();

    [DataField, AutoNetworkedField]
    public List<JobWhitelist> JobWhitelists = new();

    [DataRecord]
    [Serializable, NetSerializable]
    public readonly record struct PamphletWhitelist(string Popup, EntityWhitelist Restrictions);

    [DataRecord]
    [Serializable, NetSerializable]
    public readonly record struct JobWhitelist(LocId Popup, ProtoId<JobPrototype> JobProto);

    public bool GaveSkill;

    [DataField, AutoNetworkedField]
    public bool BypassSkill = false;
}
