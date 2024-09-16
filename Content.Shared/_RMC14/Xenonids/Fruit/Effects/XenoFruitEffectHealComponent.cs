using Robust.Shared.GameStates;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Fruit.Effects;

// Instant heal (lesser/greater resin fruit)
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitEffectHealComponent : XenoFruitEffectBaseComponent
{
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier HealAmount = default!;
}
