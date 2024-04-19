using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._CM14.Marines.Orders;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FocusOrderComponent : Component, IOrderComponent
{
    public override bool SessionSpecific => true;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new Rsi(new ResPath("/Textures/_CM14/Interface/marine_orders.rsi"), "focus");

    // TODO CM14 Make this do something when/if you will ever be able to modify the deviation of bullets with an event
    // or something. Trying to do it now would be possible but would then deviate from upstream stuff.
    [DataField, AutoNetworkedField]
    public FixedPoint2 AccuracyModifier = 1.1;

    // TODO CM14 Make this do something when/if weapons ever get range.
    [DataField, AutoNetworkedField]
    public FixedPoint2 RangeModifier = 1.1;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan Duration { get; set; }

    public void AssignMultiplier(FixedPoint2 multiplier)
    {
        AccuracyModifier *= multiplier;
        RangeModifier *= multiplier;
    }
}
