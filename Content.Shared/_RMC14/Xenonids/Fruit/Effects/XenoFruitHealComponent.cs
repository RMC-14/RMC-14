using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Fruit.Effects;

// Instant heal (lesser/greater resin fruit)
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitHealComponent : XenoFruitBaseComponent
{
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 HealAmount = default!;
}
