using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

// TODO RMC14: cm13 also grants this from failed eye surgery, the autodoc & EMPed thermal goggles,
// & cures it with eye surgery & the autodoc
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, Access(typeof(RMCVisionDisabilitySystem))]
public sealed partial class RMCNearsightedComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextCheck;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan CarrotJuiceStart;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan LastCarrotJuice;
}
