using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Armor;

/// <summary>
///     A component which toggles lights off whenever the user dies, gets infected or devoured.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCSuitLightSystem))]
public sealed partial class RMCSuitLightComponent : Component;
