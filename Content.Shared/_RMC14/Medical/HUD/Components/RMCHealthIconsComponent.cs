using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.HUD.Components;

/// <summary>
/// Defines the health icons shown on this entity with a medical hud
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCHealthIconsComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, ProtoId<HealthIconPrototype>> Icons = new()
    {
        ["Healthy"] = "CMHealthIconHealthy",
        ["DeadDefib"] = "CMHealthIconDeadDefib",
        ["DeadClose"] = "CMHealthIconDeadClose",
        ["DeadAlmost"] = "CMHealthIconDeadAlmost",
        ["DeadDNR"] = "CMHealthIconDeadDNR",
        ["Dead"] = "CMHealthIconDead",
        ["HCDead"] = "CMHealthIconHCDead",
    };
}
