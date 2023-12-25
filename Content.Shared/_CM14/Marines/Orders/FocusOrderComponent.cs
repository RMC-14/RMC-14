using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CM14.Marines.Orders;

[RegisterComponent, NetworkedComponent]
public sealed partial class FocusOrderComponent : Component, IOrderComponent
{
    [DataField]
    public FixedPoint2 AccuracyModifier = 0.1;

    [DataField]
    public FixedPoint2 DefaultAccuracyModifier = 0.1;

    [DataField]
    public FixedPoint2 RangeModifier = 0.1;

    [DataField]
    public FixedPoint2 DefaultRangeModifier = 0.1;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Duration { get; set; }

    public void AssignMultiplier(FixedPoint2 multiplier)
    {
        AccuracyModifier = DefaultAccuracyModifier * multiplier;
        RangeModifier = DefaultRangeModifier * multiplier;
    }
    public override bool SessionSpecific => true;
}
