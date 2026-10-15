using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship;

/// <summary>
/// Marks a docking connector on the shuttle as the preferred port for restricted RMC destination routing.
/// Vanilla docking doors remain a fallback for older/event shuttle maps.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCShuttleMobileDockComponent : Component
{
}
