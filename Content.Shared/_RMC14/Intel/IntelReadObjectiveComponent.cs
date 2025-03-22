using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IntelSystem))]
public sealed partial class IntelReadObjectiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public IntelObjectiveState State = IntelObjectiveState.Inactive;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Value = FixedPoint2.New(0.1);

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillIntel";
}
