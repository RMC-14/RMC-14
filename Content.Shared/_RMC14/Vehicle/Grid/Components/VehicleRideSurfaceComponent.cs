using System.Numerics;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using RmcDrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(Content.Shared.Vehicle.VehicleRideSurfaceSystem), Other = AccessPermissions.ReadWrite)]
public sealed partial class VehicleRideSurfaceComponent : Component
{
    /// <summary>
    /// local vehicle pace areas that carry mobs standing on them
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<Box2> Bounds = new();

    /// <summary>
    /// local vehicle space areas where mobs are allowed to start climbing onto the surface
    /// falls back to bounds when empty
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<Box2> ClimbBounds = new();

    /// <summary>
    /// range from which a mob can climb onto this surface
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ClimbRange = SharedInteractionSystem.InteractionRange;

    /// <summary>
    /// time required to climb onto the surface
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ClimbDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// time required to climb down from the surface
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ClimbDownDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// draw depth used for mobs riding on top of this vehicle
    /// </summary>
    [DataField, AutoNetworkedField]
    public int RiderDrawDepth = (int) RmcDrawDepth.OverMobs + 2;

    /// <summary>
    /// soft border around the surface where walking off starts a climb down doafter
    /// riders that move past this border fall off immediately
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SoftBorderPadding = 0.75f;

    /// <summary>
    /// time a rider must keep moving out of bounds before the climb down doafter starts
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan EdgeClimbDownGrace = TimeSpan.FromSeconds(0.15);

    /// <summary>
    /// knockdown applied when a rider is forced past the soft border
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FallOffKnockdown = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// stun applied when a rider is forced past the soft border
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FallOffStun = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// max distance a rider can be carried in a single update.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxCarryDistance = 3f;

    /// <summary>
    /// whether buckled mobs can be carried by the surface
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CarryBuckled;

}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(Content.Shared.Vehicle.VehicleRideSurfaceSystem), Other = AccessPermissions.ReadWrite)]
public sealed partial class VehicleRideSurfaceRiderComponent : Component
{
    [AutoNetworkedField]
    public EntityUid Vehicle = EntityUid.Invalid;

    [AutoNetworkedField]
    public Vector2 LocalPosition;

    public DoAfterId? ClimbDownDoAfter;
    public bool ClimbDownFromEdge;
    public bool ClimbDownCompleting;
    public TimeSpan? EdgeClimbDownAt;
}

[Serializable, NetSerializable]
public sealed partial class VehicleRideSurfaceClimbDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class VehicleRideSurfaceClimbDownDoAfterEvent : DoAfterEvent
{
    [DataField]
    public bool FromEdge;

    public VehicleRideSurfaceClimbDownDoAfterEvent()
    {
    }

    public VehicleRideSurfaceClimbDownDoAfterEvent(bool fromEdge)
    {
        FromEdge = fromEdge;
    }

    public override DoAfterEvent Clone()
    {
        return new VehicleRideSurfaceClimbDownDoAfterEvent(FromEdge);
    }
}
