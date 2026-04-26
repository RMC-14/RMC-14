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
    public List<string> LandingTags = [];

    [DataField, AutoNetworkedField]
    public List<string> LandingClasses = [];

    [DataField, AutoNetworkedField]
    public bool Reserved;

    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public EntityUid? Ship;

    [DataField, AutoNetworkedField]
    public bool AutoRecall;

    [DataField, AutoNetworkedField]
    public int LightSearchRadius = 14;

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
