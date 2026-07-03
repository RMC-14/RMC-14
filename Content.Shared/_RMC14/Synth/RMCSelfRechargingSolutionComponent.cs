using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Synth;

/// <summary>
/// Slowly refills an item's solution with a fixed reagent over time.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCSelfRechargingSolutionSystem))]
public sealed partial class RMCSelfRechargingSolutionComponent : Component
{
    /// <summary>
    /// Solution to refill on the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string SolutionId = "Welder";

    /// <summary>
    /// Reagent inserted into the target solution.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> Reagent = "WeldingFuel";

    /// <summary>
    /// Amount restored per update tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 RechargeAmount = 1;

    /// <summary>
    /// Delay between recharge ticks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RechargeEvery = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Next game time when this entity should recharge.
    /// </summary>
    [DataField]
    public TimeSpan NextRecharge;
}
