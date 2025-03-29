using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.AimedShot.FocusedShooting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCFocusingComponent : Component
{
    /// <summary>
    ///     The current entity being focused on, used to add the focus visual.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid FocusTarget;

    /// <summary>
    ///     The previous focused target, used to remove the focus visual.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? OldTarget;
}
