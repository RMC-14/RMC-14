using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Fruit.Components;

// Base component for fruit effects
[Access(typeof(SharedXenoFruitSystem))]
public abstract partial class XenoFruitBaseComponent : Component { }

// Base component for fruit effects with a fixed duration
[Access(typeof(SharedXenoFruitSystem))]
public abstract partial class XenoFruitDurationComponent : XenoFruitBaseComponent
{
    // Duration of effect
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(0);
}

// Base component for fruit effects with a tick-based duration
[Access(typeof(SharedXenoFruitSystem))]
public abstract partial class XenoFruitTickBasedComponent : XenoFruitBaseComponent
{
    // Time between ticks
    [DataField]
    public TimeSpan TickPeriod = TimeSpan.FromSeconds(1);

    // Total number of ticks
    [DataField]
    public int TickCount = 0;
}


// Base classes for components applied to xenos under the effects of fruit
[Access(typeof(SharedXenoFruitSystem))]
public abstract partial class XenoFruitEffectDurationComponent : XenoFruitDurationComponent
{
    // Effect end time
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? EndAt;
}

[Access(typeof(SharedXenoFruitSystem))]
public abstract partial class XenoFruitEffectTickBasedComponent : XenoFruitTickBasedComponent
{
    // How many ticks does this effect have left?
    [DataField]
    public int? TicksLeft;

    // Time to apply next regen amount at
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? NextTickAt;
}
