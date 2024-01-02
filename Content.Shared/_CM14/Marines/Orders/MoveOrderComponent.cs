using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._CM14.Marines.Orders;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MoveOrderComponent : Component, IOrderComponent
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new Rsi(new ResPath("/Textures/_CM14/Interface/marine_orders.rsi"), "move");

    [DataField, AutoNetworkedField]
    public FixedPoint2 MoveSpeedModifier = 0.1;

    [DataField]
    public FixedPoint2 DefaultMoveSpeedModifier = 0.1;

    // CM14 TODO Actually make this do something once we got melee dodging
    [DataField, AutoNetworkedField]
    public FixedPoint2 DodgeModifier = 0.1;

    [DataField]
    public FixedPoint2 DefaultDodgeModifier = 0.1;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan Duration { get; set; }

    public void AssignMultiplier(FixedPoint2 multiplier)
    {
        MoveSpeedModifier = DefaultMoveSpeedModifier * multiplier;
        DodgeModifier = DefaultDodgeModifier * multiplier;
    }

    public override bool SessionSpecific => true;
}
