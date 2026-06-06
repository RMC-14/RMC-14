using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Pointing;

/// <summary>
/// Ents with this component will ignore points (arrows and messages) from other sources (either mobs, ghosts, or both)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCIgnorePointingComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IgnoreMobs = false;

    [DataField, AutoNetworkedField]
    public bool IgnoreGhosts = true;
}
