using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Pulling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AllowPullWhileDeadAndInfectedComponent : Component
{
    [DataField, AutoNetworkedField]
    public int InfectionStageThreshold = 1;
}
