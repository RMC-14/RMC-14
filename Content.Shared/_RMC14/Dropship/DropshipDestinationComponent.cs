using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipDestinationComponent : Component
{
    [DataField]
    public ResPath? Spawn;

    [DataField]
    public Box2? DockBounds;

    [DataField, AutoNetworkedField]
    public EntityUid? Ship;

    [DataField, AutoNetworkedField]
    public bool AutoRecall;

    [DataField, AutoNetworkedField]
    public int LightSearchRadius = 14;

    [DataField, AutoNetworkedField]
    public EntityUid? ArrivalSoundEntity;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
/// <summary>
/// Marks a dropship destination as a restricted shuttle berth and describes which shuttle profiles may use it.
/// </summary>
public sealed partial class RMCShuttleBerthComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string> Tags = [];

    [DataField, AutoNetworkedField]
    public List<string> DockClasses = [];

    [DataField, AutoNetworkedField]
    public bool Reserved = true;

    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}

/// <summary>
/// Marks a docking connector on the shuttle as the preferred port for restricted RMC berth routing.
/// Vanilla docking doors remain a fallback for older/event shuttle maps.
/// </summary>
[RegisterComponent]
public sealed partial class RMCShuttleMobileDockComponent : Component
{
}
