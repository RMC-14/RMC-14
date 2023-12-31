using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._CM14.Marines.Orders;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FocusOrderComponent : Component, IOrderComponent
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new Rsi(new ResPath("/Textures/_CM14/Interface/marine_orders.rsi"), "focus");

    [DataField, AutoNetworkedField]
    public FixedPoint2 AccuracyModifier = 0.1;

    [DataField]
    public FixedPoint2 DefaultAccuracyModifier = 0.1;

    [DataField, AutoNetworkedField]
    public FixedPoint2 RangeModifier = 0.1;

    [DataField, AutoNetworkedField]
    public FixedPoint2 DefaultRangeModifier = 0.1;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan Duration { get; set; }

    public void AssignMultiplier(FixedPoint2 multiplier)
    {
        AccuracyModifier = DefaultAccuracyModifier * multiplier;
        RangeModifier = DefaultRangeModifier * multiplier;
    }
    public override bool SessionSpecific => true;
}
