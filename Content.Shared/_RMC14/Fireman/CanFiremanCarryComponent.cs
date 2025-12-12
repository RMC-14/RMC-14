using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Fireman;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(FiremanCarrySystem))]
public sealed partial class CanFiremanCarryComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Carrying;

    [DataField, AutoNetworkedField]
    public bool AggressiveGrab;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan PullTime;

    [DataField, AutoNetworkedField]
    public TimeSpan AggressiveGrabDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillCqc";
}
