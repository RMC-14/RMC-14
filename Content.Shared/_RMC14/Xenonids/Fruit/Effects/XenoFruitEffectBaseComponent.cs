using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.EntityEffects;

namespace Content.Shared._RMC14.Xenonids.Fruit.Effects;

// Base component for fruit effects
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoFruitSystem))]
public abstract partial class XenoFruitEffectBaseComponent : Component
{
    // Duration of effect
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(0);

    // Effect end time
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? EndAt;
}
