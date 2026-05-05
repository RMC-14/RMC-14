using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Requisitions.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCBlackMarketHackingDeviceComponent : Component
{
    [DataField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillEngineer";

    [DataField]
    public int SkillLevel = 2;

    [DataField]
    public TimeSpan ProbeDelay = TimeSpan.FromSeconds(8);

    [DataField]
    public TimeSpan TuneDelay = TimeSpan.FromSeconds(8);
}
