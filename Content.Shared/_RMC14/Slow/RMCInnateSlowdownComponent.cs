using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Slow;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCInnateSlowdownComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Slowdown = 1.0f;
}
