using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Fruit.Components;

// Health regen (greater/unstable resin fruit)
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitRegenComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 RegenPerTick = 0;

    // Time between ticks
    [DataField]
    public TimeSpan TickPeriod = TimeSpan.FromSeconds(1);

    // Total number of ticks
    [DataField]
    public int TickCount = 0;
}

// Component applied to xenos under the effects of this fruit
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitEffectRegenComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 RegenPerTick = default!;

    // How many ticks does this effect have left?
    [DataField]
    public int? TicksLeft;

    // Time to apply next regen amount at
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextTickAt;

    // Time between ticks
    [DataField]
    public TimeSpan TickPeriod = TimeSpan.FromSeconds(1);

    // Total number of ticks
    [DataField]
    public int TickCount = 0;
}
