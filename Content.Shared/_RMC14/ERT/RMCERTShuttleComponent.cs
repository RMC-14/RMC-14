using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Marks a shuttle grid as belonging to an active ERT request.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCERTShuttleComponent : Component
{
    /// <summary>
    /// Request that owns this shuttle until arrival, return or cleanup.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Guid RequestId;

    /// <summary>
    /// Whether normal hijack behavior is blocked for this ERT shuttle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NoHijack = true;
}
