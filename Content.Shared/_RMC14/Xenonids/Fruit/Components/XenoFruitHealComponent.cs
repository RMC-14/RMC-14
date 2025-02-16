using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Fruit.Components;

// Instant heal (lesser/greater resin fruit)
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitHealComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 HealAmount = default!;
}
