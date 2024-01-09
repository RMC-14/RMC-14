using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._CM14.Marines.Orders;

/// <summary>
/// The Component giving a marine the ability to issue orders.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MarineOrdersComponent : Component
{
    /// <summary>
    /// The default duration of an order.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration = TimeSpan.FromSeconds(20);

    public TimeSpan Cooldown => Duration + Delay;

    /// <summary>
    /// The range of the order's effect.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int OrderRange = 8;


    /// <summary>
    /// Delay between orders
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Delay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The intensity of the order.
    /// Higher is more intense.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Multiplier = 1;


    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FocusAction = "ActionMarineFocus";

    [DataField]
    public EntityUid? FocusActionEntity;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string HoldAction = "ActionMarineHold";

    [DataField]
    public EntityUid? HoldActionEntity;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string MoveAction = "ActionMarineMove";

    [DataField]
    public EntityUid? MoveActionEntity;

    [DataField, AutoNetworkedField]
    public List<string> MoveCallouts = new() {"move-order-callout-1","move-order-callout-2","move-order-callout-3"};

    [DataField, AutoNetworkedField]
    public List<string> FocusCallouts = new() {"focus-order-callout-1","focus-order-callout-2","focus-order-callout-3"};

    [DataField, AutoNetworkedField]
    public List<string> HoldCallouts = new() {"hold-order-callout-1","hold-order-callout-2","hold-order-callout-3"};

    public override bool SessionSpecific => true;
}
