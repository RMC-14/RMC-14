using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.HUD.Components;

/// <summary>
/// Defines the health icons shown on this entity with a medical hud
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCHealthIconsComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<RMCHealthIconTypes, ProtoId<HealthIconPrototype>> Icons = new()
    {
        [RMCHealthIconTypes.Healthy] = "CMHealthIconHealthy",
        [RMCHealthIconTypes.DeadDefib] = "CMHealthIconDeadDefib",
        [RMCHealthIconTypes.DeadClose] = "CMHealthIconDeadClose",
        [RMCHealthIconTypes.DeadAlmost] = "CMHealthIconDeadAlmost",
        [RMCHealthIconTypes.DeadDNR] = "CMHealthIconDeadDNR",
        [RMCHealthIconTypes.Dead] = "CMHealthIconDead",
        [RMCHealthIconTypes.HCDead] = "CMHealthIconHCDead",
    };
}

[Serializable, NetSerializable]
public enum RMCHealthIconTypes : byte
{
    Healthy,
    DeadDefib,
    DeadClose,
    DeadAlmost,
    DeadDNR,
    Dead,
    HCDead
}
