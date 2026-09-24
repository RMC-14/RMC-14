using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weather;

/// <summary>
///     Blocks weather effects for colliding entities within this entity's sprite bounds.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCBlockWeatherComponent : Component;
