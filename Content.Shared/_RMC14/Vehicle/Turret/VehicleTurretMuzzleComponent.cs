using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleTurretMuzzleSystem))]
public sealed partial class VehicleTurretMuzzleComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Alternate = true;

    [DataField, AutoNetworkedField]
    public bool UseDirectionalOffsets = true;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetLeft = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetRight = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetLeftNorth = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetRightNorth = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetLeftEast = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetRightEast = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetLeftSouth = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetRightSouth = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetLeftWest = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetRightWest = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public bool UseRightNext;
}
