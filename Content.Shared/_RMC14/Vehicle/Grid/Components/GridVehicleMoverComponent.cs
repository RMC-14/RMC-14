using System;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.Vehicle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(Content.Shared.Vehicle.GridVehicleMoverSystem), Other = AccessPermissions.ReadWrite)]
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
    public float MaxSpeed = 11f;

    [DataField, AutoNetworkedField]
    public float Acceleration = 7f;

    [DataField, AutoNetworkedField]
    public float Deceleration = 12f;

    [DataField, AutoNetworkedField]
    public float MaxReverseSpeed = 4f;

    [DataField, AutoNetworkedField]
    public float ReverseAcceleration = 4f;

    // Distance from physics origin to the vehicle's front anchor (meters).
    [DataField, AutoNetworkedField]
    public float FrontOffset = 0f;

    [AutoNetworkedField]
    public bool IsCommittedToMove;

    [AutoNetworkedField]
    public bool IsMoving;
}
