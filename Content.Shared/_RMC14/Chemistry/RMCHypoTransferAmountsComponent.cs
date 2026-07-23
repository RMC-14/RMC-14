using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCHypoTransferAmountsComponent : Component
{
    /// <summary>
    ///     The minimum amount of solution that can be transferred at once from this solution.
    /// </summary>
    [DataField]
    public FixedPoint2 MinTransferAmount { get; set; } = FixedPoint2.New(3);

    /// <summary>
    ///     The maximum amount of solution that can be transferred at once from this solution.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxTransferAmount { get; set; } = FixedPoint2.New(30);

    [DataField, AutoNetworkedField]
    public FixedPoint2[] TransferAmounts = [3, 5, 10, 15, 30];
}
