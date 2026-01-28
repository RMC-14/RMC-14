using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Overwatch;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedOverwatchConsoleSystem))]
public sealed partial class OverwatchWatchingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Watching;

    // Entities from tiles in view of a zoomed out camera to force-send to the watcher
    public List<EntityUid>? ForcedEntities;
}
