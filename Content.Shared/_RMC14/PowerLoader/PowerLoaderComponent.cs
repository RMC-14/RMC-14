using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.PowerLoader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(PowerLoaderSystem))]
public sealed partial class PowerLoaderComponent : Component
{
    [DataField, AutoNetworkedField]
    public SkillWhitelist Skills;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> SpeedSkill = "RMCSkillPowerLoader";

    [DataField, AutoNetworkedField]
    public float SpeedPerSkill = 1.2f;

    [DataField, AutoNetworkedField]
    public string VirtualContainerId = "rmc_power_loader_cargo_virtual";

    [DataField, AutoNetworkedField]
    public EntProtoId VirtualRight = "RMCVirtualPowerLoaderRight";

    [DataField, AutoNetworkedField]
    public EntProtoId VirtualLeft = "RMCVirtualPowerLoaderLeft";

    [DataField, AutoNetworkedField]
    public DoAfter.DoAfter? DoAfter;
}
