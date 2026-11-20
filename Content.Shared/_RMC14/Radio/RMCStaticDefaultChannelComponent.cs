using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Radio;

/// <summary>
///     Add this component to headsets that should not have their default channel updated with encryption keys.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCStaticDefaultChannelComponent : Component;
