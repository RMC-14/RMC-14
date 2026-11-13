using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Actions;

/// <summary>
///     Actions with this component can skip or defer targets that share an IFF faction with the user.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCActionsSystem))]
public sealed partial class ActionTargetIFFComponent : Component
{
    /// <summary>
    /// Skip the relevant target, making it untargetable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Skip;

    /// <summary>
    /// Defer the relevant target, prefering to target non-deferred entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Defer;
}
