using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CM14.Marines.Orders;

[RegisterComponent]
public sealed partial class MoveOrderComponent : Component, IOrderComponent
{
    [DataField]
    public FixedPoint2 MoveSpeedModifier = 0.1;


    [DataField]
    public FixedPoint2 DefaultMoveSpeedModifier = 0.1;

    [DataField]
    public FixedPoint2 DodgeModifier = 0.1;

    [DataField]
    public FixedPoint2 DefaultDodgeModifier = 0.1;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Duration { get; set; }

    public void AssignMultiplier(FixedPoint2 multiplier)
    {
        MoveSpeedModifier = DefaultMoveSpeedModifier * multiplier;
        DodgeModifier = DefaultDodgeModifier * multiplier;
    }

    public override bool SessionSpecific => true;
}
