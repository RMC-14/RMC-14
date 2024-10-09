using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Fruit.Components;

// Cooldown reduction (spore resin fruit)
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitHasteComponent : XenoFruitDurationComponent
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 ReductionMax = 0.25f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 ReductionPerSlash = 0.05f;
}

// Component applied to xenos under the effects of this fruit
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitEffectHasteComponent : XenoFruitEffectDurationComponent
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 ReductionMax = default!;

    [DataField, AutoNetworkedField]
    public FixedPoint2 ReductionPerSlash = default!;

    [DataField, AutoNetworkedField]
    public FixedPoint2 ReductionCurrent = default!;
}
