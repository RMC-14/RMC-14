using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipNavigationComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillPilot";

    [DataField, AutoNetworkedField]
    public int MultiplierSkillLevel = 2;

    [DataField, AutoNetworkedField]
    public int FlyBySkillLevel = 2;

    [DataField, AutoNetworkedField]
    public float SkillFlyByMultiplier = 1.5f;

    [DataField, AutoNetworkedField]
    public float SkillTravelMultiplier = 0.5f;

    [DataField, AutoNetworkedField]
    public float SkillRechargeMultiplier = 0.75f;

    [DataField, AutoNetworkedField]
    public bool Hijackable = true;

    [DataField, AutoNetworkedField]
    public bool RemoteControl = false;

    [DataField, AutoNetworkedField]
    public TimeSpan LockoutDuration = TimeSpan.FromMinutes(10);

    [DataField, AutoNetworkedField]
    public TimeSpan LockedOutUntil = TimeSpan.Zero;
}
