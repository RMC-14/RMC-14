using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Light.Components;

/// <summary>
/// Component that causes lights to automatically turn on when the entity is initialized.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCLightStartsOnInitComponent : Component
{
}
