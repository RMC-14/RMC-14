using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMSolutionRefillerComponent), typeof(CMMedicalSupplyLinkSystem))]
public sealed partial class CMMedicalSupplyLinkComponent : Component
{
    /// <summary>
    /// The base state name for the sprite (e.g., "medlink" or "medlink_green")
    /// </summary>
    [DataField, AutoNetworkedField]
    public string BaseState = "medlink_green";

    /// <summary>
    /// When the animation completes and the link state should be updated.
    /// Null when no animation is playing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? UpdateStateAt;
}

/// <summary>
/// Visual states for medical supply link appearance.
/// </summary>
[Serializable, NetSerializable]
public enum CMMedicalSupplyLinkVisuals : byte
{
    State
}
