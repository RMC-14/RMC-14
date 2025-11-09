using Robust.Shared.GameStates;

namespace Content.Client._RMC14.Overwatch;

[RegisterComponent]
[Access(typeof(OverwatchConsoleSystem))]
public sealed partial class OverwatchRelayedSoundComponent : Component
{
    [DataField]
    public EntityUid? Relay;
}
