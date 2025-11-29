using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Stun;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCSizeStunSystem))]
public sealed partial class RMCKnockOutOnCollideComponent : Component
{
    [DataField]
    public TimeSpan ParalyzeTime;
}
