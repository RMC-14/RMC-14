using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Fireman;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(FiremanCarrySystem))]
public sealed partial class FiremanCarriableComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public bool BeingCarried;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillFireman";

    [DataField, AutoNetworkedField]
    public TimeSpan BreakDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public bool BreakingFree;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? CarrierWhitelist;

    [DataField, AutoNetworkedField]
    public bool CanThrow = false;
}
