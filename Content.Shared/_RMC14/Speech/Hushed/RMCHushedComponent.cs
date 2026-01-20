using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Speech.Hushed;

/// <summary>
/// Component that forces the entity to only whisper instead of speaking normally.
/// When trying to speak (Say), it will be converted to Whisper and a popup will be shown.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCHushedComponent : Component
{
}
