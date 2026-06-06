using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hook;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenoHookOnHitComponent : Component
{
    /// <summary>
    ///     Distance from the shooter where the hooked target should land.
    /// </summary>
    [DataField]
    public float TargetStopDistance = 1.3f; // Right in front of the shooter

    /// <summary>
    ///     The minimum distance the target will be pulled when hit.
    /// </summary>
    [DataField]
    public float MinimumHookDistance = 0.5f;
}
