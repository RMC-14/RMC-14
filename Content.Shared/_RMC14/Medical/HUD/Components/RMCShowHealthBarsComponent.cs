using Content.Shared.Damage.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.HUD.Components;

/// <summary>
/// RMC-specific health bar HUD that can show entities before the normal damage threshold.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCShowHealthBarsComponent : Component
{
    /// <summary>
    /// Damage containers that should be visible to this HUD.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<DamageContainerPrototype>> DamageContainers = new()
    {
        "Biological",
    };

    /// <summary>
    /// Optional status icon used to filter which mobs should show health bars.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<HealthIconPrototype>? HealthStatusIcon = "HealthIconFine";
}
