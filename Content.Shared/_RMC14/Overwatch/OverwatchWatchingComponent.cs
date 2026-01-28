using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Overwatch;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedOverwatchConsoleSystem))]
public sealed partial class OverwatchWatchingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Watching;

    // List of entities from tiles within view of the zoomed out camera to force-send to the watcher
    public List<EntityUid>? ForcedEntities;
    // Keeps track of which entities were force-sent, so that entities are removed from the correct watcher
    public bool isForceSent;
}
