using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines;

/// <summary>
///     This component allows you to see marine icons above mobs.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedMarineSystem))]
public sealed partial class ShowMarineIconsComponent : Component { }