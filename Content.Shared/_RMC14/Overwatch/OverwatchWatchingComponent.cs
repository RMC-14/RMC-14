using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Overwatch;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedOverwatchConsoleSystem))]
public sealed partial class OverwatchWatchingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Watching;

    // List of overridden entities from tiles within cameraRadius to be loaded for the watcher
    public List<EntityUid>? OverriddenEntities;
    // Keeps track of which entities are overridden to be loaded for which watcher, so that entities are unloaded for the correct watcher
    public bool isOverridden;
    public const float offsetAmount = 10f; // 10f matches binoculars offset
    public const float zoomAmount = 1.5f; // 1.5f matches binoculars zoom

    // Calculated camera radius while zoomed out, entities within this many tiles of the watched will load for the watcher
    // Camera radius will be equal to 28 with zoom: 1.5 and offset: 10
    public static float cameraRadius = MathF.Round(MathF.Sqrt(MathF.Pow(10.5f * zoomAmount + offsetAmount, 2) + MathF.Pow(8 * zoomAmount, 2)));
}
