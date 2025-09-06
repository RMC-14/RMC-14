using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._RMC14.Marines.Orders;

/// <summary>
/// Component for marines under the effect of the Hold Order.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HoldOrderComponent : Component, IOrderComponent
{
    [DataField, AutoNetworkedField]
    public List<(FixedPoint2 Multiplier, TimeSpan ExpiresAt)> Received { get; set; } = new();

    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new Rsi(new ResPath("/Textures/_RMC14/Interface/marine_orders.rsi"), "hold");

    /// <summary>
    /// Resistance to damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 DamageModifier = 0.05;

    [DataField]
    public List<ProtoId<DamageTypePrototype>> DamageTypes = new() { "Slash", "Blunt" };

    /// <summary>
    /// Resistance to pain.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 PainModifier = FixedPoint2.New(20);
}
