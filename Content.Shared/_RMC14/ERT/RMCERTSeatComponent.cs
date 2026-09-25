using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Seat metadata used by ERT spawning to reserve specialist seats before launch.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCERTSeatComponent : Component
{
    /// <summary>
    /// Seat tags describing which roster roles can prefer this seat.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> SeatTags = [];

    /// <summary>
    /// Role tags that should reserve this seat before generic assignment.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> ReservedRoleTags = [];

    /// <summary>
    /// Higher-priority seats are considered before lower-priority seats.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Priority;

    /// <summary>
    /// Responder currently assigned to this seat.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? OccupiedBy;

    /// <summary>
    /// Round time when a temporary seat reservation expires.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan? ReservationExpires;
}
