using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vendors;

/// <summary>
/// Entities with this component ignore the SharedSpecLimit.
/// Used by the foxtrot WS.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IgnoreSpecLimitsComponent : Component;
