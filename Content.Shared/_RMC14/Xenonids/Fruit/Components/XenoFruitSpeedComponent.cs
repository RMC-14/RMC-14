using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Fruit.Components;

// Movement speed increase (speed resin fruit)
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitSpeedComponent : Component
{
    // TODO: find appropriate value for this
    // TODO: this should (?) be a flat value, not a multiplier
    [DataField, AutoNetworkedField]
    public FixedPoint2 SpeedModifier = 0.4f;

    // Duration of effect
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(15);
}

// Component applied to xenos under the effects of this fruit
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitEffectSpeedComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 SpeedModifier = default!;

    // Effect end time
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? EndAt;

    // Duration of effect
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(0);
}
