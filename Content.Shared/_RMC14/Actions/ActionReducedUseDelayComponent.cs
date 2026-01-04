using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Actions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCActionsSystem))]
public sealed partial class ActionReducedUseDelayComponent : Component
{
    // Default cooldown without reductions
    [DataField, AutoNetworkedField]
    public TimeSpan? UseDelayBase = default!;

    // Cooldown reduction percentage
    [DataField, AutoNetworkedField]
    public FixedPoint2 UseDelayReduction = default!;
}
