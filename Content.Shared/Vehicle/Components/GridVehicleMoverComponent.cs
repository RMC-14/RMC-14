using System;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.Vehicle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(Content.Shared.Vehicle.GridVehicleMoverSystem))]
public sealed partial class GridVehicleMoverComponent : Component
{
    public bool IsSliding => SlideStart != null;

    [AutoNetworkedField]
    public Vector2i Tile;

    [AutoNetworkedField]
    public EntityCoordinates Origin;

    [AutoNetworkedField]
    public Vector2 Destination;

    [AutoNetworkedField]
    public TimeSpan? SlideStart;

    [AutoNetworkedField]
    public TimeSpan SlideDuration;

    [AutoNetworkedField]
    public TimeSpan? LastSlideEnd;

    [DataField, AutoNetworkedField]
    public TimeSpan SlideDelay = TimeSpan.FromSeconds(0.2f);

    [DataField, AutoNetworkedField]
    public float SlideSpeed = 4f;

    [DataField, AutoNetworkedField]
    public bool LinearInterp;
}
