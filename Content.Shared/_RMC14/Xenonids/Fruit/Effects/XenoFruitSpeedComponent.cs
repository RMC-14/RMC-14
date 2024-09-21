using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Fruit.Effects;

// Movement speed increase (speed resin fruit)
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitSpeedComponent : XenoFruitDurationComponent
{
    // TODO: find appropriate value for this
    [DataField, AutoNetworkedField]
    public FixedPoint2 SpeedModifier = 0.4f;
}

// Component applied to xenos under the effects of this fruit
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitEffectSpeedComponent : XenoFruitEffectDurationComponent
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 SpeedModifier = default!;
}
