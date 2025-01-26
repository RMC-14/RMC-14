using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Stun;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCStunOnTriggerComponent : Component
{
    [DataField] public float Range = 8.0f;
    [DataField] public double Duration = 27.0f;
    [DataField] public float Probability = 1.0f;
}
