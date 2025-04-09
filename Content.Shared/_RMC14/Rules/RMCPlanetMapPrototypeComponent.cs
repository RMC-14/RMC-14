using Content.Shared._RMC14.Item;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Rules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCPlanetSystem))]
public sealed partial class RMCPlanetMapPrototypeComponent : Component
{
    [DataField(required: true), AutoNetworkedField, Access(Other = AccessPermissions.ReadExecute)]
    public ResPath Map;

    [DataField, AutoNetworkedField]
    public CamouflageType Camouflage = CamouflageType.Jungle;

    [DataField, AutoNetworkedField]
    public int MinPlayers;

    [DataField(required: true), AutoNetworkedField]
    public string Announcement = string.Empty;

    [DataField, AutoNetworkedField]
    public List<(ProtoId<JobPrototype> Job, int Amount)> SurvivorJobs = new List<(ProtoId<JobPrototype> Job, int Amount)>
    {
        ("CMSurvivorEngineer", 4),
        ("CMSurvivorDoctor", 3),
        ("CMSurvivorSecurity", 2),
        ("CMSurvivorCorporate", 2),
        ("CMSurvivor", -1),
    };
}
