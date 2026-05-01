using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem))]
/// <summary>
/// Landing destination metadata used by dropship navigation and restricted ERT routing.
/// </summary>
public sealed partial class DropshipDestinationComponent : Component
{
    /// <summary>
    /// Optional map path spawned when this destination represents a shuttle template.
    /// </summary>
    [DataField]
    public ResPath? Spawn;

    /// <summary>
    /// Optional local docking bounds that incoming shuttle footprints must fit inside.
    /// </summary>
    [DataField]
    public Box2? DockBounds;

    /// <summary>
    /// Free-form landing tags used by restricted routing to allow or deny this destination.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> LandingTags = [];

    /// <summary>
    /// Docking class tags describing which shuttle profiles may use this destination.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> LandingClasses = [];

    /// <summary>
    /// Whether this destination is reserved by a pending launch or routing operation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Reserved;

    /// <summary>
    /// Whether this destination can currently be selected by navigation consoles.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Shuttle grid currently occupying this destination.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Ship;

    /// <summary>
    /// Whether the shuttle should automatically recall from this destination.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AutoRecall;

    /// <summary>
    /// Tile radius used to find landing lights associated with this destination.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int LightSearchRadius = 14;

    /// <summary>
    /// Entity used as the playback source for destination arrival sounds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ArrivalSoundEntity;
}

/// <summary>
/// Marks a docking connector on the shuttle as the preferred port for restricted RMC destination routing.
/// Vanilla docking doors remain a fallback for older/event shuttle maps.
/// </summary>
[RegisterComponent]
public sealed partial class RMCShuttleMobileDockComponent : Component
{
}
