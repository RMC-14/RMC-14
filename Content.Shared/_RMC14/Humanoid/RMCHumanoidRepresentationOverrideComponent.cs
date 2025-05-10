using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Humanoid;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCHumanoidRepresentationOverrideComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId? Species;

    [DataField, AutoNetworkedField]
    public LocId? Age;
}
