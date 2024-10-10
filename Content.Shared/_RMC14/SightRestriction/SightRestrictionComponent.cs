using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.SightRestriction;

/// <summary>
///     Component for restricting the sight of a player; this one's attached to the item
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSightRestrictionSystem))]
public sealed partial class SightRestrictionItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? User;

    [DataField, AutoNetworkedField]
    public SightRestrictionDefinition Restriction;
}

/// <summary>
///     Component for restricting the sight of a player; this one's attached to the player
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSightRestrictionSystem))]
public sealed partial class SightRestrictionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Overlay;

    // List of active restrictions
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, SightRestrictionDefinition> Restrictions;
}
