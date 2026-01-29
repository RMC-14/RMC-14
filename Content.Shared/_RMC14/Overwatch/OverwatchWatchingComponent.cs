using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Overwatch;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedOverwatchConsoleSystem))]
public sealed partial class OverwatchWatchingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Watching;

    // List of overridden entities from tiles within view of the zoomed out camera to be loaded for the watcher
    public List<EntityUid>? OverriddenEntities;
    // Keeps track of which entities are overridden to be loaded for which watcher, so that entities are unloaded for the correct watcher
    public bool isOverridden;
}
