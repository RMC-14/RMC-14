using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Pulling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCPullingSystem))]
public sealed partial class SynthStunCancelOnMoveComponent : Component
{
    [AutoNetworkedField]
    public TimeSpan CancelAfter;
}
