using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Fruit.Effects;

// Cooldown reduction (spore resin fruit)
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoFruitSystem))]
public partial class XenoFruitHasteComponent : XenoFruitDurationComponent
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 ReductionMax = 0.25f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 ReductionPerSlash = 0.05f;
}

// Component applied to xenos under the effects of this fruit
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoFruitSystem))]
public sealed partial class XenoFruitEffectHasteComponent : XenoFruitHasteComponent
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 ReductionCurrent = default!;
}
