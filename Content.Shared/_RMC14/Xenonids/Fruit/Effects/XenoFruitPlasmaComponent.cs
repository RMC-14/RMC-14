using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Fruit.Effects;

// Plasma regen (plasma resin fruit)
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitPlasmaComponent : XenoFruitTickBasedComponent
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 RegenPerTick = 0;
}

// Component applied to xenos under the effects of this fruit
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitEffectPlasmaComponent : XenoFruitEffectTickBasedComponent
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 RegenPerTick = default!;
}
