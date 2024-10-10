using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Fruit.Components;

// Cooldown reduction (spore resin fruit)
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitHasteComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 ReductionMax = 0.25f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 ReductionPerSlash = 0.05f;

    // Duration of effect
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(0);
}

// Component applied to xenos under the effects of this fruit
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitEffectHasteComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 ReductionMax = default!;

    [DataField, AutoNetworkedField]
    public FixedPoint2 ReductionPerSlash = default!;

    [DataField, AutoNetworkedField]
    public FixedPoint2 ReductionCurrent = default!;

    // Effect end time
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? EndAt;

    // Duration of effect
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(0);
}
