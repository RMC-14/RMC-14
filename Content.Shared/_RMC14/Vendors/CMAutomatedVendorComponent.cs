using System.Numerics;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Access;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Vendors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedCMAutomatedVendorSystem))]
public sealed partial class CMAutomatedVendorComponent : Component
{
    // TODO RMC14 make this EntProtoId<T>? instead of string?
    [DataField, AutoNetworkedField]
    public string? PointsType;

    [DataField, AutoNetworkedField]
    public List<ProtoId<JobPrototype>> Jobs = new();

    [DataField, AutoNetworkedField]
    public List<CMVendorSection> Sections = new();

    [DataField, AutoNetworkedField]
    public Vector2 MinOffset = new(-0.2f, -0.2f);

    [DataField, AutoNetworkedField]
    public Vector2 MaxOffset = new (0.2f, 0.2f);

    [DataField, AutoNetworkedField]
    public bool Hackable;

    [DataField, AutoNetworkedField]
    public bool Hacked;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> HackSkill = "RMCSkillEngineer";

    [DataField, AutoNetworkedField]
    public int HackSkillLevel = 2;

    [DataField, AutoNetworkedField]
    public TimeSpan HackDelay = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public List<ProtoId<AccessLevelPrototype>> Access = new();

    [DataField, AutoNetworkedField]
    public bool Scaling = true;
}
