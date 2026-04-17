using System;
using System.Numerics;
using Content.Shared._RMC14.Stun;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.Vehicle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(Content.Shared.Vehicle.GridVehicleMoverSystem), Other = AccessPermissions.ReadWrite)]
public sealed partial class GridVehicleMoverComponent : Component
{
    /// <summary>
    /// current tile occupied by the vehicle on its grid.
    /// </summary>
    [AutoNetworkedField]
    public Vector2i CurrentTile;

    /// <summary>
    /// target tile used by prediction & smoothing
    /// </summary>
    [AutoNetworkedField]
    public Vector2i TargetTile;

    /// <summary>
    /// current local grid position of the vehicle
    /// </summary>
    [AutoNetworkedField]
    public Vector2 Position;

    /// <summary>
    /// target local grid position used by prediction & smoothing
    /// </summary>
    [AutoNetworkedField]
    public Vector2 TargetPosition;

    /// <summary>
    /// current cardinal facing direction of the vehicle
    /// </summary>
    [AutoNetworkedField]
    public Vector2i CurrentDirection;

    /// <summary>
    /// current cardinal direction used when the vehicle is being pushed
    /// </summary>
    [AutoNetworkedField]
    public Vector2i PushDirection;

    /// <summary>
    /// current signed movement speed in grid units / second
    /// </summary>
    [AutoNetworkedField]
    public float CurrentSpeed;

    /// <summary>
    /// maximum forward driving speed in grid units / second
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxSpeed = 11f;

    /// <summary>
    /// forward acceleration in grid units / second squared
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Acceleration = 7f;

    /// <summary>
    /// speed loss per second when slowing down or stoping
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Deceleration = 12f;

    /// <summary>
    /// maximum reverse driving speed in grid units / second
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxReverseSpeed = 4f;

    /// <summary>
    /// reverse acceleration in grid units / second squared
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReverseAcceleration = 4f;

    /// <summary>
    /// forward offset used when placing the vehicle on tiles
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FrontOffset = 0f;

    /// <summary>
    /// maximum sideways lane offset used for normal lane correction
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TileOffsetLimit = 1f;

    /// <summary>
    /// sideways lane offset sampling step for finding clear lanes
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TileOffsetStep = 0.05f;

    /// <summary>
    /// number of tiles checked ahead when choosing a clear lane
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TileOffsetLookahead = 3;

    /// <summary>
    /// maximum sideways correction speed in grid units / second
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LaneCorrectionSpeed = 4f;

    /// <summary>
    /// distance between continuous collision probes while moving
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MovementProbeStep = 0.1f;

    /// <summary>
    /// inset applied to non-mob movement collision checks
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MovementCollisionInset = 0.05f;

    /// <summary>
    /// maximum sideways nudge distance used to bypass blocking mobs
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BlockingMobBypassNudgeLimit = 1.75f;

    /// <summary>
    /// sideways sampling step used to find a bypass around blocking mobs
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BlockingMobBypassNudgeStep = 0.1f;

    /// <summary>
    /// delay before the vehicle can be pushed again
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PushCooldown = 2f;

    /// <summary>
    /// minimum speed applied when a xeno shove starts
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PushImpulseSpeed = 0.1f;

    /// <summary>
    /// delay after changing facing direction
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TurnDelay = 0.08f;

    /// <summary>
    /// whether the vehicle can rotate without moving forward
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TurnInPlace = false;

    /// <summary>
    /// highest speed where in-place turning is allowed
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TurnInPlaceMaxSpeed = 0.35f;

    /// <summary>
    /// maximum local nudge distance when finding room to turn
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TurnNudgeLimit = 0.45f;

    /// <summary>
    /// local sampling step used when finding room to turn
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TurnNudgeStep = 0.1f;

    /// <summary>
    /// forward grace distance used to clear transient turn blockers
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TurnCollisionGraceDistance = 1f;

    /// <summary>
    /// next time this vehicle may be pushed
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan NextPushTime;

    /// <summary>
    /// next time this vehicle may turn
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan NextTurnTime;

    /// <summary>
    /// time until movement is blocked after an in-place turn
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan InPlaceTurnBlockUntil;

    /// <summary>
    /// whether movement has committed for the current simulation step
    /// </summary>
    [AutoNetworkedField]
    public bool IsCommittedToMove;

    /// <summary>
    /// whether current movement came from pushing instead of driving
    /// </summary>
    [AutoNetworkedField]
    public bool IsPushMove;

    /// <summary>
    /// whether the vehicle is currently moving
    /// </summary>
    [AutoNetworkedField]
    public bool IsMoving;

    /// <summary>
    /// minimum xeno size that blocks this vehicle
    /// </summary>
    [DataField, AutoNetworkedField]
    public RMCSizes? XenoBlockMinimumSize;

    /// <summary>
    /// whether xenos are allowed to push this vehicle
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanXenosPush = true;

    /// <summary>
    /// minimum xeno size needed to push this vehicle
    /// </summary>
    [DataField, AutoNetworkedField]
    public RMCSizes? XenoPushMinimumSize;

    /// <summary>
    /// whether this vehicle can push other grid vehicles
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanPushVehicles = false;

    /// <summary>
    /// grid that the mover state is currently synced to
    /// </summary>
    [NonSerialized]
    public EntityUid? SyncedGrid;

    /// <summary>
    /// active multiplier applied to movement speed after smashing objects
    /// </summary>
    [AutoNetworkedField]
    public float SmashSlowdownMultiplier = 1f;

    /// <summary>
    /// time when the active smash slowdown expires
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan SmashSlowdownUntil;
}
