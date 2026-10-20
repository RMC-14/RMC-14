using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship;

/// <summary>
/// Prevents dropship console actions from unbolting this door.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCDropshipDoorConsoleLockComponent : Component
{
}
