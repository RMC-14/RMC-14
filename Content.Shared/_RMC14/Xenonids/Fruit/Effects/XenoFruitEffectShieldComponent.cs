using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Fruit.Effects;

// Overshield (unstable resin fruit)
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitEffectShieldComponent : XenoFruitEffectBaseComponent
{
    // Max overshield granted
    [DataField, AutoNetworkedField]
    public FixedPoint2 ShieldAmount = 200;

    // Max ratio of shield to full health
    [DataField, AutoNetworkedField]
    public FixedPoint2 ShieldRatio = 0.3f;

    // Overshield decay rate
    [DataField, AutoNetworkedField]
    public double ShieldDecay = 10f;
}
