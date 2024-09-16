using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Fruit.Effects;

// Movement speed increase (speed resin fruit)
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitEffectSpeedComponent : XenoFruitEffectBaseComponent
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 SpeedModifier = 0.4f;
}
