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

    // TODO RMC14 implement focus order effects
    // [DataField, AutoNetworkedField]
    // public EntProtoId FocusAction = "ActionMarineFocus";
    //
    // [DataField, AutoNetworkedField]
    // public EntityUid? FocusActionEntity;

    [DataField, AutoNetworkedField]
    public EntProtoId HoldAction = "ActionMarineHold";

    [DataField, AutoNetworkedField]
    public EntityUid? HoldActionEntity;

    [DataField, AutoNetworkedField]
    public EntProtoId MoveAction = "ActionMarineMove";

    [DataField, AutoNetworkedField]
    public EntityUid? MoveActionEntity;

    [DataField, AutoNetworkedField]
    public List<LocId> MoveCallouts = new() { "move-order-callout-1", "move-order-callout-2", "move-order-callout-3" };

    [DataField, AutoNetworkedField]
    public List<LocId> FocusCallouts = new() { "focus-order-callout-1", "focus-order-callout-2", "focus-order-callout-3" };

    [DataField, AutoNetworkedField]
    public List<LocId> HoldCallouts = new() { "hold-order-callout-1", "hold-order-callout-2", "hold-order-callout-3" };

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillLeadership";
}
