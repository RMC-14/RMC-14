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
    [AutoNetworkedField]
    public Vector2i CurrentTile;

    [AutoNetworkedField]
    public Vector2i TargetTile;

    [AutoNetworkedField]
    public Vector2 Position;

    [AutoNetworkedField]
    public Vector2i CurrentDirection;

    [AutoNetworkedField]
    public float CurrentSpeed;

    [DataField, AutoNetworkedField]
    public float MaxSpeed = 15f;

    [DataField, AutoNetworkedField]
    public float Acceleration = 8f;

    [DataField, AutoNetworkedField]
    public float Deceleration = 12f;

    [AutoNetworkedField]
    public bool IsCommittedToMove;
}
