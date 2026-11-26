using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<NpcFactionPrototype>> Factions = new() { "UNMC" };

    [DataField, AutoNetworkedField]
    public bool AllSquads = true;

    [DataField, AutoNetworkedField]
    public HashSet<string> SquadGroups = new();

    [DataField, AutoNetworkedField]
    public bool LiveUpdate = true;

    [DataField, AutoNetworkedField]
    public bool CanDraw;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillLeadership";

    [DataField, AutoNetworkedField]
    public int SkillLevel = 2;

    [DataField]
    public List<Enum> UiKeys = new() { TacticalMapComputerUi.Key };

    [DataField, AutoNetworkedField]
    public List<EntityUid> Channels = new();

    [DataField, AutoNetworkedField]
    public bool Live;
}
