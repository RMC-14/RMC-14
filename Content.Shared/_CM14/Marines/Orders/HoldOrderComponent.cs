using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._CM14.Marines.Orders;

/// <summary>
/// Component for marines under the effect of the Hold Order.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HoldOrderComponent : Component, IOrderComponent
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new Rsi(new ResPath("/Textures/_CM14/Interface/marine_orders.rsi"), "hold");

    /// <summary>
    /// Resistance to damage.
    /// </summary>
    [DataField, AutoNetworkedField]
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
    [DataField, AutoNetworkedField]
    public FixedPoint2 PainModifier;

    [DataField]
    public FixedPoint2 DefaultPainModifier= 0.1;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan Duration { get; set; }

    public void AssignMultiplier(FixedPoint2 multiplier)
    {
        DamageModifier = DefaultDamageModifier * multiplier;
        PainModifier= DefaultPainModifier * multiplier;
    }
    public override bool SessionSpecific => true;
}
