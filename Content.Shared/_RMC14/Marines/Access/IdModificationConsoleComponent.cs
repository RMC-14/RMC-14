using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Access;

[RegisterComponent] [NetworkedComponent] [AutoGenerateComponentState(true)]
public sealed partial class IdModificationConsoleComponent : Component
{
    public static string PrivilegedIdCardSlotId = "IdCardConsole-privilegedId";
    public static string TargetIdCardSlotId = "IdCardConsole-targetId";

    public ProtoId<AccessLevelPrototype> Access = "RMCAccessDatabase";

    [DataField] [AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> AccessGroups = new();

    [DataField] [AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> AccessList = new();

    [DataField] [AutoNetworkedField]
    public bool Authenticated;

    [DataField] [AutoNetworkedField]
    public EntProtoId<IFFFactionComponent> Faction = "FactionMarine";

    [DataField] [AutoNetworkedField]
    public bool HasIFF;

    [DataField] [AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> HiddenAccessList = new();

    [DataField] [AutoNetworkedField]
    public HashSet<ProtoId<AccessGroupPrototype>> JobGroups = new();

    [DataField] [AutoNetworkedField]
    public HashSet<ProtoId<AccessGroupPrototype>> JobList = new();

    [DataField] [AutoNetworkedField]
    public string PrivilegedIdSlot = "PrivilegedIdSlot";

    [DataField] [AutoNetworkedField]
    public string TargetIdSlot = "TargetIdSlot";

    [DataField] [AutoNetworkedField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int>? EnlistmentRequirement = new() { ["RMCSkillFirearms"] = 1 };

    [DataField] [AutoNetworkedField]
    public string SquadGroup = "UNMC";

    [DataField] [AutoNetworkedField]
    public List<IdModificationConsoleSquads>? Squads;
}

[Serializable] [NetSerializable]
public sealed class IdModificationConsoleSquads(NetEntity id, string name, Color color)
{
    public readonly NetEntity Id = id;
    public readonly string Name = name;
    public readonly Color Color = color;
}
