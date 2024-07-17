using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Overwatch;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedOverwatchConsoleSystem))]
public sealed partial class OverwatchCameraComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Watching = new();
}
