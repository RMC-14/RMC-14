using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Fruit.Effects;

// Health regen (greater/unstable resin fruit)
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitEffectRegenComponent : XenoFruitEffectBaseComponent
{
    [DataField, AutoNetworkedField]
    public TimeSpan TickPeriod = TimeSpan.FromSeconds(1);

    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier RegenPerTick = default!;

    [DataField, AutoNetworkedField]
    public int TickCount = 0;

    // How many ticks does this effect have left?
    [DataField, AutoNetworkedField]
    public int? TicksLeft;

    // Time to apply next regen amount at
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextTickAt;
}
