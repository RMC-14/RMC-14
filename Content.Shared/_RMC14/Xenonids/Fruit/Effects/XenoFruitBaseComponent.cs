using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Fruit.Effects;

// Base component for fruit effects
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoFruitSystem))]
public abstract partial class XenoFruitBaseComponent : Component { }

// Base component for fruit effects with a fixed duration
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public abstract partial class XenoFruitDurationComponent : XenoFruitBaseComponent
{
    // Duration of effect
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(0);
}

// Base component for fruit effects with a tick-based duration
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public abstract partial class XenoFruitTickBasedComponent : XenoFruitBaseComponent
{
    // Time between ticks
    [DataField, AutoNetworkedField]
    public TimeSpan TickPeriod = TimeSpan.FromSeconds(1);

    // Total number of ticks
    [DataField, AutoNetworkedField]
    public int TickCount = 0;
}


// Base classes for components applied to xenos under the effects of fruit
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoFruitSystem))]
public abstract partial class XenoFruitEffectDurationComponent : XenoFruitDurationComponent
{
    // Effect end time
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? EndAt;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoFruitSystem))]
public abstract partial class XenoFruitEffectTickBasedComponent : XenoFruitTickBasedComponent
{
    // How many ticks does this effect have left?
    [DataField, AutoNetworkedField]
    public int? TicksLeft;

    // Time to apply next regen amount at
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextTickAt;
}
