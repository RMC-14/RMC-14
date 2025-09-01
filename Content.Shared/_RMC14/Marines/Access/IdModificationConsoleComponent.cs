using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Access;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Access;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]

public sealed partial class IdModificationConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<IFFFactionComponent> Faction = "FactionMarine";

    public static string PrivilegedIdCardSlotId = "IdCardConsole-privilegedId";
    public static string TargetIdCardSlotId = "IdCardConsole-targetId";

    [DataField, AutoNetworkedField]
    public string PrivilegedIdSlot = "PrivilegedIdSlot";

    [DataField, AutoNetworkedField]
    public string TargetIdSlot = "TargetIdSlot";

    public ProtoId<AccessLevelPrototype> Access = "RMCAccessDatabase";

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> AccessList = new();

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> HiddenAccessList = new();

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> AccessGroups = new();

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessGroupPrototype>> JobList = new();

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessGroupPrototype>> JobGroups = new();

    [DataField, AutoNetworkedField]
    public bool Authenticated = false;

    [DataField, AutoNetworkedField]
    public bool HasIFF = false;
}
