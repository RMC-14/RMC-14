using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared._RMC14.Marines.Skills;

namespace Content.Shared._RMC14.PowerLoader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(PowerLoaderSystem))]
public sealed partial class PowerLoaderGrabbableComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Delay;

    [DataField, AutoNetworkedField]
    public EntProtoId VirtualRight;

    [DataField, AutoNetworkedField]
    public EntProtoId VirtualLeft;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> AttachSkill = "RMCSkillEngineer";
}
