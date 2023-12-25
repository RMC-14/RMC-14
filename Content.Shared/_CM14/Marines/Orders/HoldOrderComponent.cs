using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CM14.Marines.Orders;

/// <summary>
/// Component for marines under the effect of the Hold Order.
/// </summary>
[RegisterComponent]
public sealed partial class HoldOrderComponent : Component, IOrderComponent
{
    /// <summary>
    /// Resistance to damage.
    /// </summary>
    [DataField]
    public FixedPoint2 DamageModifier;

    [DataField]
    public FixedPoint2 DefaultDamageModifier= 0.05;

    /// <summary>
    /// Resistance to pain.
    /// </summary>
    /// <remarks>
    /// I am unsure of when pain will be implemented but I am putting this here for the future.
    /// </remarks>
    /// CM14 TODO Make this do something meaningful when pain is actually a thing.
    [DataField]
    public FixedPoint2 PainModifier;

    [DataField]
    public FixedPoint2 DefaultPainModifier= 0.1;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Duration { get; set; }

    public void AssignMultiplier(FixedPoint2 multiplier)
    {
        DamageModifier = DefaultDamageModifier * multiplier;
        PainModifier= DefaultPainModifier * multiplier;
    }
    public override bool SessionSpecific => true;
}
