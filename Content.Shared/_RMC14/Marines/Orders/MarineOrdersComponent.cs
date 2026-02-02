using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Orders;

/// <summary>
/// The Component giving a marine the ability to issue orders.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MarineOrdersComponent : Component
{
    public override bool SessionSpecific => true;

    /// <summary>
    ///     The default duration of an order multiplied by <see cref="SkillsComponent.Leadership"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     Cooldown given to all order actions on this entity when any are pressed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(80);

    /// <summary>
    /// The range of the order's effect.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int OrderRange = 7;

    [DataField, AutoNetworkedField]
    public EntProtoId FocusAction = "ActionMarineFocus";

    [DataField, AutoNetworkedField]
    public EntityUid? FocusActionEntity;

    [DataField, AutoNetworkedField]
    public EntProtoId HoldAction = "ActionMarineHold";

    [DataField, AutoNetworkedField]
    public EntityUid? HoldActionEntity;

    [DataField, AutoNetworkedField]
    public EntProtoId MoveAction = "ActionMarineMove";

    [DataField, AutoNetworkedField]
    public EntityUid? MoveActionEntity;

    [DataField, AutoNetworkedField]
    public List<LocId> MoveCallouts = new() { "move-order-callout-1", "move-order-callout-2", "move-order-callout-3", "move-order-callout-4", "move-order-callout-5", "move-order-callout-6", "move-order-callout-7", "move-order-callout-8", "move-order-callout-9", "move-order-callout-10", "move-order-callout-11", "move-order-callout-12", "move-order-callout-13", "move-order-callout-14", "move-order-callout-15" };

    [DataField, AutoNetworkedField]
    public List<LocId> FocusCallouts = new() { "focus-order-callout-1", "focus-order-callout-2", "focus-order-callout-3", "focus-order-callout-4", "focus-order-callout-5", "focus-order-callout-6", "focus-order-callout-7", "focus-order-callout-8", "focus-order-callout-9", "focus-order-callout-10", "focus-order-callout-11", "focus-order-callout-12", "focus-order-callout-13", "focus-order-callout-14", "focus-order-callout-15", "focus-order-callout-16", "focus-order-callout-17", "focus-order-callout-18", "focus-order-callout-19" };

    [DataField, AutoNetworkedField]
    public List<LocId> HoldCallouts = new() { "hold-order-callout-1", "hold-order-callout-2", "hold-order-callout-3", "hold-order-callout-4", "hold-order-callout-5", "hold-order-callout-6", "hold-order-callout-7", "hold-order-callout-8", "hold-order-callout-9", "hold-order-callout-10", "hold-order-callout-11", "hold-order-callout-12", "hold-order-callout-13", "hold-order-callout-14", "hold-order-callout-15", "hold-order-callout-16", "hold-order-callout-17" };

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillLeadership";

    [DataField, AutoNetworkedField]
    public bool Intrinsic = true;
}
