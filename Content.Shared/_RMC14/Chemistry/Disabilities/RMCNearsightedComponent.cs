using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

// TODO RMC14: cm13 also grants this from failed eye surgery, the autodoc & EMPed thermal goggles,
// & cures it with eye surgery & the autodoc
[RegisterComponent, NetworkedComponent, Access(typeof(RMCVisionDisabilitySystem))]
public sealed partial class RMCNearsightedComponent : Component
{
    [DataField]
    public TimeSpan NextCheck;

    [DataField]
    public TimeSpan CarrotJuiceStart;

    [DataField]
    public TimeSpan LastCarrotJuice;
}
