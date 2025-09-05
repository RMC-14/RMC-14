using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Vendors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMAutomatedVendorSystem))]
public sealed partial class CMVendorUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<JobPrototype>? Id;

    [DataField, AutoNetworkedField]
    public Dictionary<string, int> Choices = new();

    [DataField, AutoNetworkedField]
    public HashSet<(string Category, EntProtoId Ent)> TakeAll = new();

    [DataField, AutoNetworkedField]
    public HashSet<string> TakeOne = new();

    [DataField, AutoNetworkedField]
    public int Points;

    [DataField, AutoNetworkedField]
    public Dictionary<string, int>? ExtraPoints;
}
